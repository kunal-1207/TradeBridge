using TradeBridge.Contracts.Enums;

namespace TradeBridge.Contracts.Orders;

public class OrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string ClientOrderId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string? Message { get; set; }
}
