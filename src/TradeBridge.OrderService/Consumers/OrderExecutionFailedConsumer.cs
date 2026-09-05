using MassTransit;
using TradeBridge.Contracts.Enums;
using TradeBridge.Contracts.Messages;
using TradeBridge.OrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeBridge.OrderService.Consumers;

public class OrderExecutionFailedConsumer : IConsumer<OrderExecutionFailed>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderExecutionFailedConsumer> _logger;

    public OrderExecutionFailedConsumer(OrderDbContext dbContext, ILogger<OrderExecutionFailedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderExecutionFailed> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Order {ClientOrderId} execution failed: {Reason}", msg.ClientOrderId, msg.Reason);

        if (!Guid.TryParse(msg.OrderId, out var orderId))
        {
            _logger.LogError("Invalid OrderId format {OrderId}", msg.OrderId);
            return;
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", msg.OrderId);
            return;
        }

        // Idempotency: Check if already processed
        if (order.Status == OrderStatus.FAILED || order.Status == OrderStatus.REJECTED || order.Status == OrderStatus.FILLED)
        {
            _logger.LogInformation("Order {OrderId} is already in a terminal state ({Status}). Ignoring message.", msg.OrderId, order.Status);
            return;
        }

        order.Status = OrderStatus.FAILED;
        order.Reason = msg.Reason;

        await _dbContext.SaveChangesAsync();
    }
}
