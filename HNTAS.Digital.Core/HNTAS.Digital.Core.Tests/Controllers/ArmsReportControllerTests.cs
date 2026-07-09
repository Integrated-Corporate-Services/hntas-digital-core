using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms.PowerBi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;


namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class ArmsReportControllerTests
    {
        private readonly Mock<IArmsPowerBiService> _mockService;
        private readonly Mock<ILogger<ArmsReportController>> _mockLogger;
        private readonly ArmsReportController _controller;

        public ArmsReportControllerTests()
        {
            _mockService = new Mock<IArmsPowerBiService>();
            _mockLogger = new Mock<ILogger<ArmsReportController>>();

            _controller = new ArmsReportController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPowerBiData_ReturnsOk_WithCombinedAndFlattenedData()
        {
            // Arrange
            var mockData = new List<ArmsPowerBiReportResult>
                        {
                            new ArmsPowerBiReportResult
                            {
                                OrgId = "org-123",
                                KpiSubmission = new KpiSubmission
                                {
                                    MetaData = new KpiMetadata
                                    {
                                        NetworkId = "hn-456",
                                        PeriodStart = "2026-01"
                                    },
                                    Elements = new List<NetworkElement>
                                    {
                                        new NetworkElement
                                        {
                                            ElementId = "elem-1",
                                            Type = HeatNetworkElementType.EnergyCentre,
                                            Kpis = new Dictionary<string, KpiValue>
                                            {
                                                { "KPI_01", new KpiValue { Value = 100, AssessmentStatus = KPIAssessmentStatus.Pass } }
                                            }
                                        }
                                    },
                                    ConsumerConnectionAggregatedKpis = null
                                }
                            }
                        };

            _mockService.Setup(s => s.GetPowerBiDataAsync())
                        .ReturnsAsync(mockData);

            // Act
            var result = await _controller.GetPowerBiData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseList = Assert.IsAssignableFrom<List<ArmsPowerBiReportResponse>>(okResult.Value);

            Assert.Single(responseList);
            Assert.Equal("elem-1", responseList[0].ElementId);
            Assert.Equal("org-123", responseList[0].OrgId);
            Assert.Equal(100, responseList[0].Value); // Assert string comparison if Value is mapped as string
        }

        [Fact]
        public async Task GetPowerBiData_ReturnsOk_WithEmptyList_WhenServiceReturnsNullOrEmpty()
        {
            // Arrange: Cover the scenario where service returns no data (null)
            List<ArmsPowerBiReportResult> mockNullData = null;

            _mockService.Setup(s => s.GetPowerBiDataAsync())
                        .ReturnsAsync(mockNullData);

            // Act
            var result = await _controller.GetPowerBiData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseList = Assert.IsAssignableFrom<List<ArmsPowerBiReportResponse>>(okResult.Value);

            Assert.Empty(responseList); // Verifies that your safeguard returns an empty list, not a 500 error

            // Verify warning log was called for empty data
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No data returned from ArmsPowerBiService")),
                    null,
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetPowerBiData_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerBiDataAsync())
                        .ThrowsAsync(new Exception("Database connectivity failure"));

            // Act
            var result = await _controller.GetPowerBiData();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error retrieving data for Power BI.", statusCodeResult.Value);

            // Verify that the error log was captured
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unexpected error occurred")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}