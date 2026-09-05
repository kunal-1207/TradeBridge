using TradeBridge.Shared.Logging;
using TradeBridge.Shared.HealthChecks;
using TradeBridge.Shared.Polly;
using TradeBridge.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging("ApiGateway");
builder.Services.AddStandardOpenTelemetry("ApiGateway");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var orderServiceUrl = builder.Configuration.GetValue<string>("ServiceUrls:OrderService") ?? "http://localhost:5000";

builder.Services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri(orderServiceUrl);
})
.AddStandardResiliencePolicies();

builder.Services.AddStandardHealthChecks();

var app = builder.Build();

app.UseStandardOpenTelemetry();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapStandardHealthChecks();

app.Run();
