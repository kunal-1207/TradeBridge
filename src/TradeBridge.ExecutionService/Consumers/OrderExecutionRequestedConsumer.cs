using MassTransit;
using System.Net.Http.Json;
using TradeBridge.Contracts.Liquidity;
using TradeBridge.Contracts.Messages;

namespace TradeBridge.ExecutionService.Consumers;

public class OrderExecutionRequestedConsumer : IConsumer<OrderExecutionRequested>
{
    private readonly ILogger<OrderExecutionRequestedConsumer> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _liquidityProviderUrl;

    public OrderExecutionRequestedConsumer(
        ILogger<OrderExecutionRequestedConsumer> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("LiquidityProvider");
        _liquidityProviderUrl = configuration.GetValue<string>("ServiceUrls:LiquidityProvider") ?? "http://localhost:5002";
    }

    public async Task Consume(ConsumeContext<OrderExecutionRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("ExecutionService processing Order {ClientOrderId}", msg.ClientOrderId);

        var lpRequest = new LiquidityOrderRequest
        {
            OrderId = msg.OrderId,
            Symbol = msg.Symbol,
            Side = msg.Side,
            Quantity = msg.Quantity,
            Price = msg.Price
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_liquidityProviderUrl}/orders", lpRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LiquidityOrderResponse>();
                if (result != null && result.Success)
                {
                    _logger.LogInformation("Order {ClientOrderId} filled at LP", msg.ClientOrderId);
                    await context.Publish<OrderExecutionCompleted>(new
                    {
                        msg.OrderId,
                        msg.ClientOrderId,
                        result.ProviderOrderId
                    });
                    return;
                }

                _logger.LogWarning("Order {ClientOrderId} failed at LP: {Reason}", msg.ClientOrderId, result?.Reason);
                await context.Publish<OrderExecutionFailed>(new
                {
                    msg.OrderId,
                    msg.ClientOrderId,
                    Reason = result?.Reason ?? "LP_REJECTED"
                });
            }
            else
            {
                _logger.LogError("LP returned {StatusCode} for {ClientOrderId}", response.StatusCode, msg.ClientOrderId);
                await context.Publish<OrderExecutionFailed>(new
                {
                    msg.OrderId,
                    msg.ClientOrderId,
                    Reason = "LP_HTTP_ERROR"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection error to LP for {ClientOrderId}", msg.ClientOrderId);
            // In a real scenario, we might throw to trigger MassTransit retries
            // For now, publish failed event
            await context.Publish<OrderExecutionFailed>(new
            {
                msg.OrderId,
                msg.ClientOrderId,
                Reason = "LP_CONNECTION_ERROR"
            });
        }
    }
}
