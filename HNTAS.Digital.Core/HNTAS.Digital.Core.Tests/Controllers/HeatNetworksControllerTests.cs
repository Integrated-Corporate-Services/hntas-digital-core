using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class HeatNetworksControllerTests
    {
        private readonly Mock<IHeatNetworkService> _mockHnService;
        private readonly Mock<ICounterService> _mockCounterService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<HeatNetworksController>> _mockLogger;
        private readonly HeatNetworksController _controller;

        public HeatNetworksControllerTests()
        {
            _mockHnService = new Mock<IHeatNetworkService>();
            _mockCounterService = new Mock<ICounterService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<HeatNetworksController>>();

            // Assuming these dependencies are injected via the constructor in your partial class
            _controller = new HeatNetworksController(_mockHnService.Object, _mockLogger.Object, _mockCounterService.Object, _mockMapper.Object);
        }

        private HeatNetwork SampleHeatNetwork(string id = "1", string hnId = null)
        {
            return new HeatNetwork
            {
                Id = id,
                HnId = hnId,
                Location = "LocationA",
                Name = "Network A",
                Pathway = "Pathway X",
                CreatedBy = "tester",
                CreatedAt = DateTime.UtcNow
            };
        }

        private HeatNetworkResponse SampleHeatNetworkResponse(string id = "1", string hnId = "HN0000001")
        {
            return new HeatNetworkResponse
            {
                Id = id,
                HnId = hnId,
                Location = "LocationA",
                Name = "Network A",
                Pathway = "Pathway X",
                Soa = null
            };
        }

        // 1) GetHeatNetworks - Positive
        [Fact]
        public async Task GetHeatNetworks_ReturnsOk_WithList()
        {
            // Arrange
            var domainList = new List<HeatNetwork> { SampleHeatNetwork("1", "HN0000001") };
            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetAsync()).ReturnsAsync(domainList);
            _mockMapper.Setup(m => m.Map<List<HeatNetworkResponse>>(It.IsAny<List<HeatNetwork>>()))
                       .Returns(responseList);

            var controller = new HeatNetworksController(_hnServiceMock.Object, _loggerMock.Object, _counterServiceMock.Object, _mapperMock.Object);

            // Act
            var result = await _controller.GetHeatNetworks();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<HeatNetworkResponse>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("HN0000001", returned[0].HnId);
        }

        // 1) GetHeatNetworks - Negative (exception -> 500)
        [Fact]
        public async Task GetHeatNetworks_OnException_Returns500()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetAsync()).ThrowsAsync(new Exception("DB failure"));

            var controller = new HeatNetworksController(_hnServiceMock.Object, _loggerMock.Object, _counterServiceMock.Object, _mapperMock.Object);

            // Act
            var result = await _controller.GetHeatNetworks();

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objResult.StatusCode);
        }

        // 2) GetHeatNetworksByHnIds - Positive
        [Fact]
        public async Task GetHeatNetworksByHnIds_WithValidIds_ReturnsOk()
        {
            // Arrange
            var hnIdsString = "HN0000001,HN0000002";
            var ids = hnIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var domainList = new List<HeatNetwork> { SampleHeatNetwork("1", "HN0000001") };
            var responseList = new List<HeatNetworkResponse> { SampleHeatNetworkResponse("1", "HN0000001") };

            _mockHnService.Setup(s => s.GetByHnIdsAsync(It.Is<List<string>>(l => l.SequenceEqual(ids))))
                          .ReturnsAsync(domainList);

            _mockMapper.Setup(m => m.Map<List<HeatNetworkResponse>>(It.IsAny<List<HeatNetwork>>()))
                       .Returns(responseList);

            var controller = new HeatNetworksController(_hnServiceMock.Object, _loggerMock.Object, _counterServiceMock.Object, _mapperMock.Object);

            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<List<HeatNetworkResponse>>(ok.Value);
            Assert.Single(returned);
            Assert.Equal("HN0000001", returned[0].HnId);
        }

        // 2) GetHeatNetworksByHnIds - Negative (bad request when no ids)
        [Fact]
        public async Task GetHeatNetworksByHnIds_WithNoIds_ReturnsBadRequest()
        {
            // Arrange
            string hnIdsString = null; // controller will interpret as no ids

            var controller = new HeatNetworksController(_hnServiceMock.Object, _loggerMock.Object, _counterServiceMock.Object, _mapperMock.Object);

            // Act
            var result = await _controller.GetHeatNetworksByHnIds(hnIdsString);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        // 3) GetHeatNetworkByHnId - Positive
        [Fact]
        public async Task GetHeatNetworkByHnId_WithValidId_ReturnsOk()
        {
            // Arrange
            var hnId = "HN0000001";
            var domain = SampleHeatNetwork("1", hnId);
            var response = SampleHeatNetworkResponse("1", hnId);

            _mockHnService.Setup(s => s.GetByHnIdAsync(hnId)).ReturnsAsync(domain);
            _mockMapper.Setup(m => m.Map<HeatNetworkResponse>(It.IsAny<HeatNetwork>())).Returns(response);

            // Act
            var result = await _controller.GetHeatNetworkByHnId(hnId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<HeatNetworkResponse>(ok.Value);
            Assert.Equal(hnId, returned.HnId);
        }

        // 3) GetHeatNetworkByHnId - Negative (invalid input -> BadRequest)
        [Fact]
        public async Task GetHeatNetworkByHnId_WithEmptyId_ReturnsBadRequest()
        {
            // Arrange
            var controller = new HeatNetworksController(_hnServiceMock.Object, _loggerMock.Object, _counterServiceMock.Object, _mapperMock.Object);

            // Act
            var result = await _controller.GetHeatNetworkByHnId(string.Empty);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        // 4) AddHeatNetwork - Positive (generates HnId and creates)
        [Fact]
        public async Task AddHeatNetwork_WithoutHnId_GeneratesHnIdAndReturnsCreated()
        {
            // Arrange
            var input = SampleHeatNetwork("1", hnId: null); // no HnId set
            _mockCounterService.Setup(c => c.GetNextSequenceValue("heatNetworkId_sequence")).ReturnsAsync(1L);
            _mockHnService.Setup(s => s.CreateAsync(It.IsAny<HeatNetwork>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddHeatNetwork(input);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returned = Assert.IsAssignableFrom<HeatNetwork>(created.Value);
            Assert.NotNull(returned.HnId);
            Assert.StartsWith("HN", returned.HnId);
        }

        // 4) AddHeatNetwork - Negative (service throws -> 500)
        [Fact]
        public async Task AddHeatNetwork_OnCreateException_Returns500()
        {
            // Arrange
            var input = SampleHeatNetwork("1", "HN0000001");
            _mockHnService.Setup(s => s.CreateAsync(It.IsAny<HeatNetwork>())).ThrowsAsync(new Exception("write failed"));

            // Act
            var result = await _controller.AddHeatNetwork(input);

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objResult.StatusCode);
            Assert.IsType<ProblemDetails>(objResult.Value);
        }



        #region GetExternalHeatNetworks Tests

        [Fact]
        public async Task GetExternalHeatNetworks_ReturnsOk_WithData()
        {
            // Arrange
            var mockNetworks = new List<HeatNetwork> { new HeatNetwork() };
            var mockResponse = new List<HeatNetworkResponse> { new HeatNetworkResponse() };

            _mockHnService.Setup(s => s.GetAsync()).ReturnsAsync(mockNetworks);
            _mockMapper.Setup(m => m.Map<List<HeatNetworkResponse>>(mockNetworks)).Returns(mockResponse);

            // Act
            var result = await _controller.GetExternalHeatNetworks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(mockResponse, okResult.Value);
        }

        [Fact]
        public async Task GetExternalHeatNetworks_Returns500_OnException()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetAsync()).ThrowsAsync(new System.Exception());

            // Act
            var result = await _controller.GetExternalHeatNetworks();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetExternalHeatNetworkById Tests

        [Fact]
        public async Task GetExternalHeatNetworkById_ReturnsNotFound_WhenNull()
        {
            // Arrange
            _mockHnService.Setup(s => s.GetByHnIdAsync("123")).ReturnsAsync((HeatNetwork)null);

            // Act
            var result = await _controller.GetExternalHeatNetworkById("123");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetExternalHeatNetworkById_ReturnsOk_WhenFound()
        {
            // Arrange
            var network = new HeatNetwork { Id = "123" };
            var response = new HeatNetworkResponse { Id = "123" };
            _mockHnService.Setup(s => s.GetByHnIdAsync("123")).ReturnsAsync(network);
            _mockMapper.Setup(m => m.Map<HeatNetworkResponse>(network)).Returns(response);

            // Act
            var result = await _controller.GetExternalHeatNetworkById("123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, okResult.Value);
        }

        #endregion


        #region GetExternalHeatNetworksByDate Tests

        [Fact]
        public async Task GetExternalHeatNetworksByDate_ReturnsBadRequest_WhenDatesInvalid()
        {
            // Arrange
            var from = DateTime.Now.AddDays(1);
            var to = DateTime.Now;

            // Act
            var result = await _controller.GetExternalHeatNetworksByDate(from, to);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("The 'fromDate' cannot be after the 'toDate'.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetExternalHeatNetworksByDate_ReturnsOk_WithValidRange()
        {
            // Arrange
            var from = DateTime.Now.AddDays(-1);
            var to = DateTime.Now;
            _mockHnService.Setup(s => s.GetByDateRangeAsync(from, to))
                        .ReturnsAsync(new List<HeatNetwork>());

            // Act
            var result = await _controller.GetExternalHeatNetworksByDate(from, to);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        #endregion
    }
}