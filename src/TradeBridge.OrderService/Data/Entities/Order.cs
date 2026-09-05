using TradeBridge.Contracts.Enums;

namespace TradeBridge.OrderService.Data.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string ClientOrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public OrderStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? ProviderOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
