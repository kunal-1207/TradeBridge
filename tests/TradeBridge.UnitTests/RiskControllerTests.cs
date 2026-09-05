using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TradeBridge.Contracts.Risk;
using TradeBridge.RiskService.Controllers;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace TradeBridge.UnitTests;

public class RiskControllerTests
{
    [Fact]
    public void Check_QuantityExceedsMax_ReturnsNotApproved()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();

        configMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(sectionMock.Object);
        sectionMock.Setup(s => s.Value).Returns((string)null); // Default fallback

        var controller = new RiskController(new NullLogger<RiskController>(), configMock.Object);

        var request = new RiskCheckRequest
        {
            ClientOrderId = "123",
            Quantity = 2000000, // Make it explicitly larger than default 1,000,000
            Price = 1.0m
        };

        // Act
        var result = controller.Check(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RiskCheckResponse>(okResult.Value);
        Assert.False(response.Approved);
        Assert.Equal("MAX_ORDER_QUANTITY_EXCEEDED", response.Reason);
    }

    [Fact]
    public void Check_ValidRequest_ReturnsApproved()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();

        configMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(sectionMock.Object);
        sectionMock.Setup(s => s.Value).Returns((string)null); // Default fallback

        var controller = new RiskController(new NullLogger<RiskController>(), configMock.Object);

        var request = new RiskCheckRequest
        {
            ClientOrderId = "123",
            Quantity = 100,
            Price = 1.0m
        };

        // Act
        var result = controller.Check(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RiskCheckResponse>(okResult.Value);
        Assert.True(response.Approved);
    }
}
