using TradeBridge.Contracts.Enums;

namespace TradeBridge.Contracts.Messages;

public interface OrderExecutionRequested
{
    string OrderId { get; }
    string ClientOrderId { get; }
    string Symbol { get; }
    OrderSide Side { get; }
    decimal Quantity { get; }
    decimal Price { get; }
}

public interface OrderExecutionCompleted
{
    string OrderId { get; }
    string ClientOrderId { get; }
    string ProviderOrderId { get; }
}

public interface OrderExecutionFailed
{
    string OrderId { get; }
    string ClientOrderId { get; }
    string Reason { get; }
}
