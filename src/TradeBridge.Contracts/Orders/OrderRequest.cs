using System.ComponentModel.DataAnnotations;
using TradeBridge.Contracts.Enums;

namespace TradeBridge.Contracts.Orders;

public class OrderRequest
{
    [Required]
    public string ClientOrderId { get; set; } = string.Empty;

    [Required]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public OrderSide Side { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0.00001, double.MaxValue)]
    public decimal Price { get; set; }
}
