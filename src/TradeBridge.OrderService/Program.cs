using Microsoft.EntityFrameworkCore;
using TradeBridge.OrderService.Data;
using TradeBridge.Shared.Logging;
using TradeBridge.Shared.HealthChecks;
using TradeBridge.Shared.Polly;
using TradeBridge.Shared.Observability;
using MassTransit;
using TradeBridge.OrderService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging("OrderService");
builder.Services.AddStandardOpenTelemetry("OrderService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("Services")
    .AddStandardResiliencePolicies();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<OrderExecutionCompletedConsumer>();
    x.AddConsumer<OrderExecutionFailedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost";
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMq:Username") ?? "guest");
            h.Password(builder.Configuration.GetValue<string>("RabbitMq:Password") ?? "guest");
        });

        cfg.ReceiveEndpoint("order-execution-completed", e =>
        {
            e.ConfigureConsumer<OrderExecutionCompletedConsumer>(context);
        });

        cfg.ReceiveEndpoint("order-execution-failed", e =>
        {
            e.ConfigureConsumer<OrderExecutionFailedConsumer>(context);
        });
    });
});

builder.Services.AddStandardHealthChecks();

var app = builder.Build();

app.UseStandardOpenTelemetry();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate(); // Auto-migrate for demo purposes
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapStandardHealthChecks();

app.Run();
