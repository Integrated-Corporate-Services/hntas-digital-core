
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers;

public class AuditControllerTests
{
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        _auditServiceMock = new Mock<IAuditService>();
        _controller = new AuditController(_auditServiceMock.Object);
    }

    [Fact]
    public async Task GetHeatNetworkHistory_ShouldReturnBadRequest_WhenHnIdIsNull()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            HnId = null
        };

        // Act
        var result = await _controller.GetHeatNetworkHistory(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A valid Heat Network ID is required.", badRequestResult.Value);

        _auditServiceMock.Verify(
            x => x.GetAuditHistoryAsync<HeatNetwork>(It.IsAny<AuditLogRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHeatNetworkHistory_ShouldReturnBadRequest_WhenHnIdIsEmpty()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            HnId = string.Empty
        };

        // Act
        var result = await _controller.GetHeatNetworkHistory(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A valid Heat Network ID is required.", badRequestResult.Value);

        _auditServiceMock.Verify(
            x => x.GetAuditHistoryAsync<HeatNetwork>(It.IsAny<AuditLogRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHeatNetworkHistory_ShouldReturnBadRequest_WhenHnIdIsWhitespace()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            HnId = "   "
        };

        // Act
        var result = await _controller.GetHeatNetworkHistory(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A valid Heat Network ID is required.", badRequestResult.Value);

        _auditServiceMock.Verify(
            x => x.GetAuditHistoryAsync<HeatNetwork>(It.IsAny<AuditLogRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHeatNetworkHistory_ShouldReturnNotFound_WhenNoHistoryExists()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            HnId = "HN001"
        };

        _auditServiceMock
            .Setup(x => x.GetAuditHistoryAsync<HeatNetwork>(It.IsAny<AuditLogRequest>()))
            .ReturnsAsync((AuditLogResponse)null);

        // Act
        var result = await _controller.GetHeatNetworkHistory(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        var value = notFoundResult.Value;
        var messageProperty = value.GetType().GetProperty("message");

        Assert.NotNull(messageProperty);

        var message = messageProperty.GetValue(value)?.ToString();

        Assert.Equal(
            "No audit history found for Heat Network: HN001",
            message);

        _auditServiceMock.Verify(
            x => x.GetAuditHistoryAsync<HeatNetwork>(It.IsAny<AuditLogRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetHeatNetworkHistory_ShouldReturnOk_WhenHistoryExists()
    {
        // Arrange
        var request = new AuditLogRequest
        {
            HnId = "HN001"
        };

        var response = new AuditLogResponse();

        _auditServiceMock
            .Setup(x => x.GetAuditHistoryAsync<HeatNetwork>(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetHeatNetworkHistory(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, okResult.Value);

        _auditServiceMock.Verify(
            x => x.GetAuditHistoryAsync<HeatNetwork>(request),
            Times.Once);
    }
}