using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradeBridge.Contracts.Liquidity;

namespace TradeBridge.LiquidityProvider.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly string _failureMode;
    private readonly int _latencyMs;

    public OrdersController(ILogger<OrdersController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _failureMode = configuration.GetValue<string>("FAILURE_MODE", "NORMAL")!.ToUpperInvariant();
        _latencyMs = configuration.GetValue<int>("LATENCY_MS", 1500);
    }

    [HttpPost]
    public async Task<ActionResult<LiquidityOrderResponse>> CreateOrder([FromBody] LiquidityOrderRequest request)
    {
        _logger.LogInformation("Received liquidity order for {OrderId}, Mode: {FailureMode}", request.OrderId, _failureMode);

        switch (_failureMode)
        {
            case "HIGH_LATENCY":
                await Task.Delay(_latencyMs);
                break;
            case "TIMEOUT":
                await Task.Delay(TimeSpan.FromMinutes(5)); // Simulate a hang that exceeds normal timeouts
                break;
            case "HTTP_500":
                return StatusCode(500, "Simulated internal server error");
            case "HTTP_429":
                return StatusCode(429, "Simulated rate limit exceeded");
            case "UNAVAILABLE":
                return StatusCode(503, "Simulated service unavailable");
            case "NORMAL":
            default:
                // Normal processing (maybe small realistic latency)
                await Task.Delay(50);
                break;
        }

        _logger.LogInformation("Successfully processed liquidity order {OrderId}", request.OrderId);
        return Ok(new LiquidityOrderResponse
        {
            OrderId = request.OrderId,
            ProviderOrderId = Guid.NewGuid().ToString(),
            Success = true
        });
    }
}
