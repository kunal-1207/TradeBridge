using Microsoft.AspNetCore.Mvc;
using TradeBridge.Contracts.Orders;

namespace TradeBridge.ApiGateway.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IHttpClientFactory httpClientFactory, ILogger<OrdersController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OrderService");
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        _logger.LogInformation("API Gateway received order {ClientOrderId}", request.ClientOrderId);

        var response = await _httpClient.PostAsJsonAsync("/api/v1/orders", request);

        var content = await response.Content.ReadAsStringAsync();
        return StatusCode((int)response.StatusCode, content);
    }

    [HttpGet("{clientOrderId}")]
    public async Task<IActionResult> GetOrder(string clientOrderId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/orders/{clientOrderId}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        return StatusCode((int)response.StatusCode);
    }
}
