using TradeBridge.ExecutionService;

using MassTransit;
using TradeBridge.ExecutionService.Consumers;
using TradeBridge.Shared.Logging;
using TradeBridge.Shared.HealthChecks;
using TradeBridge.Shared.Polly;
using TradeBridge.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging("ExecutionService");
builder.Services.AddStandardOpenTelemetry("ExecutionService");

builder.Services.AddHttpClient("LiquidityProvider")
    .AddStandardResiliencePolicies();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderExecutionRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost";
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMq:Username") ?? "guest");
            h.Password(builder.Configuration.GetValue<string>("RabbitMq:Password") ?? "guest");
        });

        cfg.ReceiveEndpoint("order-execution-requested", e =>
        {
            e.ConfigureConsumer<OrderExecutionRequestedConsumer>(context);
        });
    });
});

builder.Services.AddStandardHealthChecks();

var app = builder.Build();

app.UseStandardOpenTelemetry();

app.MapStandardHealthChecks();

app.Run();
