using MassTransit;
using TradeBridge.Contracts.Enums;
using TradeBridge.Contracts.Messages;
using TradeBridge.OrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace TradeBridge.OrderService.Consumers;

public class OrderExecutionCompletedConsumer : IConsumer<OrderExecutionCompleted>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderExecutionCompletedConsumer> _logger;

    public OrderExecutionCompletedConsumer(OrderDbContext dbContext, ILogger<OrderExecutionCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderExecutionCompleted> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Order {ClientOrderId} execution completed", msg.ClientOrderId);

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
        if (order.Status == OrderStatus.FILLED)
        {
            _logger.LogInformation("Order {OrderId} is already filled. Ignoring message.", msg.OrderId);
            return;
        }

        order.Status = OrderStatus.FILLED;
        order.ProviderOrderId = msg.ProviderOrderId;

        await _dbContext.SaveChangesAsync();
    }
}
