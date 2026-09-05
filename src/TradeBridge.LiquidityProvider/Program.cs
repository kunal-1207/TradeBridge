using TradeBridge.Shared.Logging;
using TradeBridge.Shared.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging("LiquidityProvider");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStandardHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapStandardHealthChecks();

app.Run();
