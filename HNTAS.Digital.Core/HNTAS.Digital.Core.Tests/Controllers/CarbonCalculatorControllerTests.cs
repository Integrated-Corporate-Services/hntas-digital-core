using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class CarbonCalculatorControllerTests
    {
        private readonly Mock<ICarbonCalculatorService> _mockService;
        private readonly CarbonCalculatorController _controller;

        public CarbonCalculatorControllerTests()
        {
            _mockService = new Mock<ICarbonCalculatorService>();
            _controller = new CarbonCalculatorController(_mockService.Object);
        }

        [Fact]
        public async Task RunAsync_WhenCalculationSucceeds_ReturnsOkWithResult()
        {
            // Arrange
            var request = new CarbonCalculatorRequest();
            var expectedResponse = new CarbonCalculatorResponse
            {
                HnId = "HN-12345",
                Uuid = "mock-uuid-999",
                TotalCarbonEmission = 120.45m
            };

            _mockService
                .Setup(s => s.RunAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var actionResult = await _controller.RunAsync(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedModel = Assert.IsType<CarbonCalculatorResponse>(okResult.Value);

            Assert.Equal("HN-12345", returnedModel.HnId);
            Assert.Equal("mock-uuid-999", returnedModel.Uuid);
            Assert.Equal(120.45m, returnedModel.TotalCarbonEmission);
        }

        [Fact]
        public async Task RunAsync_WhenServiceReturnsNull_ReturnsProblemDetails()
        {
            // Arrange
            var request = new CarbonCalculatorRequest();

            // Simulate a failed calculation or missing token by returning null
            _mockService
                .Setup(s => s.RunAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CarbonCalculatorResponse?)null);

            // Act
            var actionResult = await _controller.RunAsync(request, CancellationToken.None);

            // Assert
            // ASP.NET Core's Problem() method helper outputs an ObjectResult containing ProblemDetails
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);

            // Check that it uses the default internal server error status code (500)
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Calculation failed or API token missing.", problemDetails.Detail);
        }
    }
}