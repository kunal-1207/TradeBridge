using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradeBridge.Shared.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddStandardHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("live", () => HealthCheckResult.Healthy("Service is live"), tags: new[] { "live" })
            .AddCheck("ready", () => HealthCheckResult.Healthy("Service is ready"), tags: new[] { "ready" });

        return services;
    }

    public static WebApplication MapStandardHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });

        app.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        return app;
    }
}
