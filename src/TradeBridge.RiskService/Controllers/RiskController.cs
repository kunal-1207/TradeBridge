using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradeBridge.Contracts.Risk;

namespace TradeBridge.RiskService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RiskController : ControllerBase
{
    private readonly ILogger<RiskController> _logger;
    private readonly decimal _maxOrderQuantity;
    private readonly decimal _maxPriceDeviation;

    public RiskController(ILogger<RiskController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _maxOrderQuantity = configuration.GetValue<decimal>("Risk:MaxOrderQuantity", 1000000m);
        _maxPriceDeviation = configuration.GetValue<decimal>("Risk:MaxPriceDeviation", 0.05m);
    }

    [HttpPost("check")]
    public ActionResult<RiskCheckResponse> Check([FromBody] RiskCheckRequest request)
    {
        _logger.LogInformation("Performing risk check for client order {ClientOrderId}", request.ClientOrderId);

        if (request.Quantity > _maxOrderQuantity)
        {
            _logger.LogWarning("Risk check failed for {ClientOrderId}: Quantity exceeds maximum", request.ClientOrderId);
            return Ok(new RiskCheckResponse { Approved = false, Reason = "MAX_ORDER_QUANTITY_EXCEEDED" });
        }

        // Simplistic price deviation check against a hypothetical market price for demo purposes
        // In reality, this would query a market data service
        if (request.Price <= 0)
        {
            _logger.LogWarning("Risk check failed for {ClientOrderId}: Invalid price", request.ClientOrderId);
            return Ok(new RiskCheckResponse { Approved = false, Reason = "INVALID_PRICE" });
        }

        _logger.LogInformation("Risk check passed for client order {ClientOrderId}", request.ClientOrderId);
        return Ok(new RiskCheckResponse { Approved = true });
    }
}
