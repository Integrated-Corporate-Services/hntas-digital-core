using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class CountriesAndTerritoriesControllerTests
    {
        private readonly Mock<ICountryAndTerritoryService> _mockService;
        private readonly Mock<ILogger<CountriesAndTerritoriesController>> _mockLogger;
        private readonly CountriesAndTerritoriesController _controller;

        public CountriesAndTerritoriesControllerTests()
        {
            _mockService = new Mock<ICountryAndTerritoryService>();
            _mockLogger = new Mock<ILogger<CountriesAndTerritoriesController>>();

            _controller = new CountriesAndTerritoriesController(
                _mockLogger.Object,
                _mockService.Object
            );
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsOk_WithListOfCountries()
        {
            // Arrange
            var mockData = new List<CountryAndTerritory>
            {
                new CountryAndTerritory { Name = "Abu Dhabi", FullValue = "territory:AE-AZ" },
                new CountryAndTerritory { Name = "Afghanistan", FullValue = "country:AF" }
            };

            _mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(mockData);

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<CountryAndTerritory>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
            Assert.Equal("Abu Dhabi", returnedList[0].Name);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOk_WhenListIsEmpty()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<CountryAndTerritory>());

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<CountryAndTerritory>>(okResult.Value);
            Assert.Empty(returnedList);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync())
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("Internal server error", objectResult.Value);
        }

        #endregion
    }
}
