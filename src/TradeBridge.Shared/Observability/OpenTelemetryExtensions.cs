using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TradeBridge.Shared.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddStandardOpenTelemetry(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
                tracing.AddSource("MassTransit"); // MassTransit has built-in OpenTelemetry source

                tracing.AddZipkinExporter(options =>
                {
                    // Zipkin endpoint could be configured via env var in production
                    options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddMeter("MassTransit"); // MassTransit metrics
                metrics.AddPrometheusExporter();
            });

        return services;
    }

    public static WebApplication UseStandardOpenTelemetry(this WebApplication app)
    {
        // Expose /metrics endpoint for Prometheus to scrape
        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
