using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class HNDataImportExportControllerTests
    {

        private readonly Mock<IHNDataImportExportService> _mockService;
        private readonly HNDataImportExportController _controller;

        public HNDataImportExportControllerTests()
        {
            _mockService = new Mock<IHNDataImportExportService>();
            _controller = new HNDataImportExportController(_mockService.Object);
        }

        [Fact]
        public async Task GetJson_ReturnsOk_WithRows()
        {
            // Arrange
            var rows = new List<HeatNetworkExportRow>
            {
                new HeatNetworkExportRow
                {
                    UserEmailId = "user@test.com",
                    OrganisationName = "Test Org",
                    HnId = "HN001",
                    HnName = "Test Heat Network"
                }
            };

            _mockService
                .Setup(s => s.GetAllHeatNetworkRowsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            // Act
            var result = await _controller.GetJson();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<List<HeatNetworkExportRow>>(okResult.Value);

            Assert.Single(value);
            Assert.Equal("HN001", value[0].HnId);
        }

        [Fact]
        public async Task GetCsv_ReturnsCsvFile()
        {
            // Arrange
            var rows = new List<HeatNetworkExportRow>
                        {
                            new HeatNetworkExportRow
                            {
                                UserEmailId = "user@test.com",
                                OrganisationName = "Test Org",
                                OrganisationId = "ORG1",
                                HnId = "HN001",
                                HnName = "Test Heat Network"
                            }
                        };

            _mockService
                .Setup(s => s.GetAllHeatNetworkRowsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            // Act
            var result = await _controller.GetCsv();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
            Assert.EndsWith(".csv", fileResult.FileDownloadName);

            var csv = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
            Assert.Contains("UserEmailId,OneloginId,OrganisationName", csv);
            Assert.Contains("user@test.com", csv);
        }


        [Fact]
        public async Task GetCsv_WithTake_LimitsNumberOfRows()
        {
            // Arrange
            var rows = new List<HeatNetworkExportRow>
            {
                new() { UserEmailId = "user1@test.com", HnId = "HN1" },
                new() { UserEmailId = "user2@test.com", HnId = "HN2" }
            };

            _mockService
                .Setup(s => s.GetAllHeatNetworkRowsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);

            // Act
            var result = await _controller.GetCsv(take: 1);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            var csv = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

            Assert.Contains("user1@test.com", csv);
            Assert.DoesNotContain("user2@test.com", csv);
        }

        [Fact]
        public async Task GetCsv_WhenNoRows_ReturnsHeaderOnly()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetAllHeatNetworkRowsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HeatNetworkExportRow>());

            // Act
            var result = await _controller.GetCsv();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            var csv = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

            Assert.Contains("UserEmailId,OneloginId,OrganisationName", csv);
        }

    }
}
