using Asp.Versioning;
using Fcs.Bff.WebApi.Proxy;
using Fcs.Bff.WebApi.Settings;
using Fcs.Bff.WebApi.Swagger;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Fcs.Bff.WebApi;

[ExcludeFromCodeCoverage]
public sealed class Program
{
    private Program()
    {
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddBffSwagger(builder.Configuration);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<ProxyService>();

        builder.Services
            .AddOptions<DownstreamServicesOptions>()
            .Bind(builder.Configuration.GetRequiredSection(DownstreamServicesOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<CorsSettings>()
            .Bind(builder.Configuration.GetRequiredSection(CorsSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<ObservabilitySettings>()
            .Bind(builder.Configuration.GetRequiredSection(ObservabilitySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var downstream = builder.Configuration
            .GetRequiredSection(DownstreamServicesOptions.SectionName)
            .Get<DownstreamServicesOptions>()
            ?? throw new InvalidOperationException("Downstream services settings are required.");

        builder.Services.AddHttpClient(DownstreamServiceNames.Identity, client =>
        {
            client.BaseAddress = new Uri(downstream.IdentityBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddHttpClient(DownstreamServiceNames.Campaign, client =>
        {
            client.BaseAddress = new Uri(downstream.CampaignBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddHttpClient(DownstreamServiceNames.Donations, client =>
        {
            client.BaseAddress = new Uri(downstream.DonationsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        var cors = builder.Configuration
            .GetRequiredSection(CorsSettings.SectionName)
            .Get<CorsSettings>()
            ?? new CorsSettings();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(cors.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        AddObservability(builder);

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseSerilogRequestLogging();
        app.UseCorrelationId();
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Unexpected BFF error."
                });
            });
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            }
        });
        app.MapPrometheusScrapingEndpoint("/metrics");

        var enableHttpsRedirection = builder.Configuration.GetValue("HttpsRedirection:Enabled", true);
        if (!app.Environment.IsDevelopment()
            && app.Environment.EnvironmentName != "Docker"
            && enableHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.MapControllers();
        app.Run();
    }

    private static void AddObservability(WebApplicationBuilder builder)
    {
        var settings = builder.Configuration
            .GetRequiredSection(ObservabilitySettings.SectionName)
            .Get<ObservabilitySettings>()
            ?? throw new InvalidOperationException("Observability settings are required.");

        var environment = builder.Environment.EnvironmentName;
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(settings.ServiceName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment
            });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (settings.EnableOtlpExporter && !string.IsNullOrWhiteSpace(settings.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{settings.OtlpEndpoint}/v1/traces");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        options.Headers = string.IsNullOrWhiteSpace(settings.OtlpAuthHeader)
                            ? string.Empty
                            : $"Authorization={settings.OtlpAuthHeader}";
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (settings.EnableOtlpExporter && !string.IsNullOrWhiteSpace(settings.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{settings.OtlpEndpoint}/v1/metrics");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                        options.Headers = string.IsNullOrWhiteSpace(settings.OtlpAuthHeader)
                            ? string.Empty
                            : $"Authorization={settings.OtlpAuthHeader}";
                    });
                }
            });

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.With<TraceContextEnricher>()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "Fcs.Bff")
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");

        if (settings.EnableOtlpExporter && !string.IsNullOrWhiteSpace(settings.OtlpEndpoint))
        {
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = $"{settings.OtlpEndpoint}/v1/logs";
                options.Protocol = OtlpProtocol.HttpProtobuf;
                options.Headers = string.IsNullOrWhiteSpace(settings.OtlpAuthHeader)
                    ? []
                    : new Dictionary<string, string> { ["Authorization"] = settings.OtlpAuthHeader };
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = settings.ServiceName,
                    ["deployment.environment"] = environment
                };
            });
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        builder.Host.UseSerilog();
    }
}
