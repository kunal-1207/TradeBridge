using TradeBridge.Shared.Logging;
using TradeBridge.Shared.HealthChecks;
using TradeBridge.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging("RiskService");
builder.Services.AddStandardOpenTelemetry("RiskService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
