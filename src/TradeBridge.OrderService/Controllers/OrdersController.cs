using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using TradeBridge.Contracts.Enums;
using TradeBridge.Contracts.Messages;
using TradeBridge.Contracts.Orders;
using TradeBridge.Contracts.Risk;
using TradeBridge.OrderService.Data;
using TradeBridge.OrderService.Data.Entities;

namespace TradeBridge.OrderService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;
    private readonly HttpClient _httpClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _riskServiceUrl;

    public OrdersController(
        OrderDbContext dbContext,
        ILogger<OrdersController> logger,
        IHttpClientFactory httpClientFactory,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Services");
        _publishEndpoint = publishEndpoint;
        _riskServiceUrl = configuration.GetValue<string>("ServiceUrls:RiskService") ?? "http://localhost:5001";
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderRequest request)
    {
        _logger.LogInformation("Received order {ClientOrderId}", request.ClientOrderId);

        // Idempotency check
        var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.ClientOrderId == request.ClientOrderId);
        if (existingOrder != null)
        {
            _logger.LogInformation("Order {ClientOrderId} already exists, returning existing status.", request.ClientOrderId);
            return Ok(MapToResponse(existingOrder));
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientOrderId = request.ClientOrderId,
            Symbol = request.Symbol,
            Side = request.Side,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = OrderStatus.RECEIVED
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        order.Status = OrderStatus.VALIDATED;
        await _dbContext.SaveChangesAsync();

        // 1. Risk Check
        _logger.LogInformation("Calling Risk Service for {ClientOrderId}", order.ClientOrderId);
        var riskRequest = new RiskCheckRequest
        {
            ClientOrderId = order.ClientOrderId,
            Symbol = order.Symbol,
            Side = order.Side,
            Quantity = order.Quantity,
            Price = order.Price
        };

        var riskResponse = await _httpClient.PostAsJsonAsync($"{_riskServiceUrl}/api/v1/risk/check", riskRequest);

        if (!riskResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Risk service call failed for {ClientOrderId}", order.ClientOrderId);
            order.Status = OrderStatus.FAILED;
            order.Reason = "RISK_SERVICE_ERROR";
            await _dbContext.SaveChangesAsync();
            return StatusCode(500, MapToResponse(order));
        }

        var riskResult = await riskResponse.Content.ReadFromJsonAsync<RiskCheckResponse>();
        if (riskResult == null || !riskResult.Approved)
        {
            _logger.LogWarning("Order {ClientOrderId} rejected by Risk Service: {Reason}", order.ClientOrderId, riskResult?.Reason);
            order.Status = OrderStatus.REJECTED;
            order.Reason = riskResult?.Reason ?? "REJECTED_BY_RISK";
            await _dbContext.SaveChangesAsync();
            return BadRequest(MapToResponse(order));
        }

        order.Status = OrderStatus.RISK_CHECKED;
        await _dbContext.SaveChangesAsync();

        // 2. Publish to message broker
        order.Status = OrderStatus.SUBMITTED;

        _logger.LogInformation("Publishing OrderExecutionRequested for {ClientOrderId}", order.ClientOrderId);

        await _publishEndpoint.Publish<OrderExecutionRequested>(new
        {
            OrderId = order.Id.ToString(),
            ClientOrderId = order.ClientOrderId,
            Symbol = order.Symbol,
            Side = order.Side,
            Quantity = order.Quantity,
            Price = order.Price
        });

        await _dbContext.SaveChangesAsync();

        // Respond early, state will be updated asynchronously
        return Ok(MapToResponse(order));
    }

    [HttpGet("{clientOrderId}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(string clientOrderId)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.ClientOrderId == clientOrderId);
        if (order == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(order));
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            OrderId = order.Id.ToString(),
            ClientOrderId = order.ClientOrderId,
            Status = order.Status,
            Message = order.Reason
        };
    }
}
