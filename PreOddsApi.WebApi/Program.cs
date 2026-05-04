
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
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

var builder = WebApplication.CreateBuilder(args);

IWebHostEnvironment environment = builder.Environment;


DependencyService.SetDependencyTypes(builder.Services, builder.Configuration);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Optimal);
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins("http://localhost:28333") // İzin verilen orijinleri buraya ekleyin
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

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

app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


