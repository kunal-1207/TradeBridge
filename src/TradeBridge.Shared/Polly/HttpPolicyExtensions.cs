using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace TradeBridge.Shared.Polly;

public static class HttpPolicyExtensions
{
    public static IHttpClientBuilder AddStandardResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler((serviceProvider, request) => GetRetryPolicy(serviceProvider))
            .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider)
    {
        return global::Polly.Extensions.Http.HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var logger = provider.GetService<ILogger<HttpClient>>();
                    logger?.LogWarning("Delaying for {delay}ms, then making retry {retry}. Reason: {reason}",
                        timespan.TotalMilliseconds, retryAttempt, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return global::Polly.Extensions.Http.HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
