
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using PreOddsApi.BusinessLayer.DependencyInjection;
using PreOddsApi.WebApi;
using System;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

IWebHostEnvironment environment = builder.Environment;


DependencyService.SetDependencyTypes(builder.Services, builder.Configuration);
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
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IUserDataService,
    PreOddsApi.WebApi.V3.Data.PostgresUserDataService>();
builder.Services.AddSingleton<PreOddsApi.WebApi.V3.Data.IAnalyticsReader,
    PreOddsApi.WebApi.V3.Data.PostgresAnalyticsReader>();

builder.Services.AddHealthChecks()
    .AddCheck<PreOddsApi.WebApi.V3.Health.PostgresHealthCheck>(
        "postgres", tags: new[] { "ready" });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
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

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddMvc();
builder.Services.AddAutoMapper(typeof(MappingProfile));


builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options => {
    var supportedCultures = new[]
    {
                    new CultureInfo("en"),
                    new CultureInfo("tr"),
                };

    options.DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var jwtSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET")
                ?? builder.Configuration["Authentication:JwtSecret"]
                ?? "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";
var jwtIssuer = builder.Configuration["Authentication:Issuer"] ?? "http://localhost:28332";
var jwtAudience = builder.Configuration["Authentication:Audience"] ?? "http://localhost:28332";

if (!builder.Environment.IsDevelopment() &&
    (jwtSecret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || jwtSecret.Length < 32))
{
    throw new InvalidOperationException(
        "A strong PREODDS_JWT_SECRET or Authentication:JwtSecret value is required outside Development.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerOptions =>
{
    JwtBearerOptions.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
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

var app = builder.Build();
IConfiguration configuration = app.Configuration;

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred.");
        throw;
    }
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

app.MapControllers();
app.MapHub<PreOddsApi.WebApi.V3.Hubs.LiveHub>("/hubs/live");

app.Run();


