using TradeBridge.Contracts.Enums;

namespace TradeBridge.Contracts.Risk;

public class RiskCheckRequest
{
    public string ClientOrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}
