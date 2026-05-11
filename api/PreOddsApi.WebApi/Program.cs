
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;
using PreOddsApi.BusinessLayer.DependencyInjection;
using PreOddsApi.WebApi;
using PreOddsApi.WebApi.V3.Helpers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog bootstrap logger — captures the very early bootstrap exceptions
// before the host is built. The "real" logger is configured below from
// appsettings via ReadFrom.Configuration.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// MachineName / EnvironmentName enrichers live in Serilog.Enrichers.Environment,
// which we're not adding right now — keep deps lean. FromLogContext is enough
// for correlation id propagation, which is the load-bearing piece.
//
// Sentry sink: Warning+ events get shipped to Sentry alongside Console.
// DSN comes from SENTRY_DSN env var. When unset (dev / CI), the Sentry
// SDK no-ops — Console output stays unchanged.
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}");

    if (!string.IsNullOrWhiteSpace(sentryDsn))
    {
        cfg.WriteTo.Sentry(o =>
        {
            o.Dsn = sentryDsn;
            o.MinimumEventLevel = Serilog.Events.LogEventLevel.Warning;
            o.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Information;
            o.Environment = ctx.HostingEnvironment.EnvironmentName;
        });
    }
});

// AspNetCore-side Sentry middleware: captures unhandled exceptions on the
// request pipeline (the Serilog.Sentry sink only catches what's logged),
// stamps each event with the request's TraceIdentifier so it lines up
// with our Serilog correlation id.
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = sentryDsn;
        o.Environment = builder.Environment.EnvironmentName;
        // Trace 10% of requests in prod; nothing in development.
        o.TracesSampleRate = builder.Environment.IsDevelopment() ? 0.0 : 0.1;
        o.AttachStacktrace = true;
        // PII off by default — we don't want auth headers / cookies in
        // breadcrumbs. Flip to true only with deliberate consent flow.
        o.SendDefaultPii = false;
    });
}

IWebHostEnvironment environment = builder.Environment;


DependencyService.SetDependencyTypes(builder.Services, builder.Configuration);

// One Npgsql connection pool for the whole process. Every V3 reader/service
// pulls connections out of this pool instead of constructing a fresh
// NpgsqlConnection per request. Multiplexing is enabled so concurrent
// queries share physical connections — meaningful win on the analytics
// endpoints that fan out per fixture.
var pgConnectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
    ?? builder.Configuration.GetConnectionString("PreOddsApiPostgresDb")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string 'PreOddsApiPostgresDb' is required.");
{
    var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(pgConnectionString);
    // Multiplexing: concurrent commands share a single physical connection
    // when possible. Safe for read-heavy workloads; writers also benefit.
    dataSourceBuilder.ConnectionStringBuilder.Multiplexing = true;
    builder.Services.AddSingleton(dataSourceBuilder.Build());
}

builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IOddsReader,
    PreOddsApi.WebApi.V3.Data.PostgresOddsReader>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IAppSchemaService,
    PreOddsApi.WebApi.V3.Data.PostgresAppSchemaService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.ISyncDiagnostics,
    PreOddsApi.WebApi.V3.Data.PostgresSyncDiagnostics>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IReferenceDataReader,
    PreOddsApi.WebApi.V3.Data.PostgresReferenceDataReader>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IFixtureReader,
    PreOddsApi.WebApi.V3.Data.PostgresFixtureReader>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IStandingsNewsReader,
    PreOddsApi.WebApi.V3.Data.PostgresStandingsNewsReader>();
builder.Services.AddScoped<PreOddsApi.WebApi.V3.Data.IUserIdentityService,
    PreOddsApi.WebApi.V3.Data.PostgresUserIdentityService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IRefreshTokenService,
    PreOddsApi.WebApi.V3.Data.PostgresRefreshTokenService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IAccountTokenService,
    PreOddsApi.WebApi.V3.Data.PostgresAccountTokenService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IUserDataService,
    PreOddsApi.WebApi.V3.Data.PostgresUserDataService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IGuestQuotaService,
    PreOddsApi.WebApi.V3.Data.PostgresGuestQuotaService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IAnalyticsReader,
    PreOddsApi.WebApi.V3.Data.PostgresAnalyticsReader>();

builder.Services.AddHealthChecks()
    .AddCheck<PreOddsApi.WebApi.V3.Health.PostgresHealthCheck>(
        "postgres", tags: new[] { "ready" })
    // Sync freshness is "ready"-tagged but degrades to non-failing on stale
    // — we don't want a stuck worker to drop /ready and yank the load
    // balancer; the read path is still live. Dashboards alert on Degraded.
    .AddCheck<PreOddsApi.WebApi.V3.Health.SyncFreshnessHealthCheck>(
        "sync_freshness",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "ready", "data" });

builder.Services.AddRateLimiter(options =>
{
    // "auth" — login / refresh / signup are highest-abuse targets. 10/min/IP.
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // "read-heavy" — analytics signals, prematch odds, fixture detail. The
    // SQL behind these is expensive (multi-CTE + window functions). Cap to
    // 60 req/min/user, queue burst of 20.
    options.AddFixedWindowLimiter("read-heavy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 60;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 20;
    });

    // "write" — coupons, favorites, devices, preferences. Modest cap to stop
    // a buggy client from flooding writes.
    options.AddFixedWindowLimiter("write", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 30;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Global IP fallback for un-policied endpoints — stops a single IP from
    // exhausting the server while we tag the rest of the controllers.
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<
        Microsoft.AspNetCore.Http.HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 200,
                QueueLimit = 50,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));

    options.RejectionStatusCode = 429;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v3", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PreOdds API",
        Version = "v3",
        Description = "PreOdds read API — odds, fixtures, markets."
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// .NET 8's System.Text.Json with a snake_case naming policy reproduces the
// same wire format we previously got from Newtonsoft + per-property
// [JsonProperty("snake_case")]. Faster (~2-3×) and one fewer dep.
// JsonStringEnumConverter so enums round-trip as their member name rather
// than the integer ordinal.
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    opt.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services.AddMvc();
builder.Services.AddAutoMapper(typeof(MappingProfile));


// Legacy ASP.NET Core resource-based localization for the deleted
// WebUI/Razor controllers — V3 doesn't use resx (mobile owns
// translations via i18next). Removed alongside the legacy controllers.

// AuthOptions is the single source of truth for JWT issuer/audience/secret/
// lifetimes. AuthController and JwtBearer both read from the same singleton
// so they can never drift.
var authOptions = PreOddsApi.WebApi.V3.Auth.AuthOptions.Load(builder.Configuration);

if (!builder.Environment.IsDevelopment() && authOptions.IsDefaultSecret)
{
    throw new InvalidOperationException(
        "A strong PREODDS_JWT_SECRET or Authentication:JwtSecret value is required outside Development.");
}

builder.Services.AddSingleton(authOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerOptions =>
{
    JwtBearerOptions.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = authOptions.Issuer,
        ValidAudience = authOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSecret))
    };
});

var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsAllowedOrigins.Length == 0)
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }
            else
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must be configured outside Development.");
            }
        }
        else
        {
            policy.WithOrigins(corsAllowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Optimal);
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<GzipCompressionProvider>();
});

// SignalR + live broadcaster.
builder.Services.AddSignalR();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Hubs.ILiveBroadcaster,
                              PreOddsApi.WebApi.V3.Hubs.LiveBroadcaster>();

// OpenTelemetry: ASP.NET Core, HttpClient, Npgsql, runtime + Prometheus
// scrape endpoint at /metrics. Tracing spans are emitted to the same
// pipeline; if you wire OTLP later, both metrics and traces flow through.
const string ServiceName = "PreOddsApi.WebApi";
var otelResource = ResourceBuilder.CreateDefault()
    .AddService(ServiceName, serviceVersion: "1.0")
    .AddAttributes(new[] { new System.Collections.Generic.KeyValuePair<string, object>(
        "deployment.environment", builder.Environment.EnvironmentName) });

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Npgsql")
        .AddPrometheusExporter())
    .WithTracing(t => t
        .SetResourceBuilder(otelResource)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // .AddNpgsql() collides with Npgsql.EntityFrameworkCore.PostgreSQL's
        // identically-named extension transitively pulled by other projects
        // in the solution. Subscribing to the source by name yields the
        // same telemetry without the ambiguity.
        .AddSource("Npgsql"));

var app = builder.Build();
IConfiguration configuration = app.Configuration;

// Stamp every request with a correlation id BEFORE Serilog request logging
// so the id is in scope when UseSerilogRequestLogging emits the access log.
app.UseMiddleware<CorrelationIdMiddleware>();

// Structured access log: one line per request with method/path/status/elapsed.
// Replaces the bare try/catch we had — uncaught exceptions are still logged
// because UseSerilogRequestLogging marks the request "Error" and Serilog
// captures the exception.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0}ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Items.TryGetValue("CorrelationId", out var corr) && corr is string id)
            diagnosticContext.Set("CorrelationId", id);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v3/swagger.json", "PreOdds API v3"));
}

app.UseCors();

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

// Prometheus scrape endpoint. Behind the same JWT-anonymous fence as
// /health — locked down via reverse-proxy or network policy in prod.
app.MapPrometheusScrapingEndpoint();

app.MapControllers();
app.MapHub<PreOddsApi.WebApi.V3.Hubs.LiveHub>("/hubs/live");

app.Run();

// Top-level statements compile into an internal Program class; tests
// using WebApplicationFactory<Program> need a public surface.
public partial class Program { }


