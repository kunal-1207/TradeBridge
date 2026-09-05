namespace TradeBridge.Contracts.Liquidity;

public class LiquidityOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string ProviderOrderId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Reason { get; set; }
}
