using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class ImportControllerTests
    {
        private readonly Mock<ICsvImportService> _mockCsvImportService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<ImportController>> _mockLogger;
        private readonly ImportController _controller;

        public ImportControllerTests()
        {
            _mockCsvImportService = new Mock<ICsvImportService>();
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<ImportController>>();

            _controller = new ImportController(
                _mockCsvImportService.Object,
                _mockUserService.Object,
                _mockLogger.Object,
                _mockEmailService.Object);
        }

        [Fact]
        public async Task UploadCsv_ReturnsBadRequest_WhenFileContentIsEmpty()
        {
            // Act
            var result = await _controller.UploadCsv(string.Empty, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UploadCsv_ReturnsOk_WhenImportSucceeds()
        {
            // Arrange
            var importResult = new ImportResult
            {
                RowsProcessed = 10
            };

            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResult);

            // Act
            var result = await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);

            var response = Assert.IsType<ImportResult>(okResult.Value);
            Assert.Equal(10, response.RowsProcessed);
        }

        [Fact]
        public async Task UploadCsv_SendsExistingOrgEmails_WhenDataExists()
        {
            // Arrange
            var importResult = new ImportResult
            {
                DataForExistingOrgOrUser =
                [
                    new OfgemDataModelForNotification()
                ]
            };

            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResult);

            // Act
            await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            _mockEmailService.Verify(
                x => x.TrySendOfgemDataForExistingOrgOrRpEmailAsync(
                    It.IsAny<OfgemDataModelForNotification>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadCsv_SendsNewRpEmails_WhenDataExists()
        {
            // Arrange
            var importResult = new ImportResult
            {
                DataForNewOrgOrUser =
                [
                    new OfgemDataModelForNotification()
                ]
            };

            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResult);

            // Act
            await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            _mockEmailService.Verify(
                x => x.TrySendOfgemDataForNewRpEmailAsync(
                    It.IsAny<OfgemDataModelForNotification>()),
                Times.Once);
        }

        [Fact]
        public async Task UploadCsv_ClearsNotificationLists_BeforeReturning()
        {
            // Arrange
            var importResult = new ImportResult
            {
                DataForExistingOrgOrUser =
                [
                    new OfgemDataModelForNotification()
                ],
                DataForNewOrgOrUser =
                [
                    new OfgemDataModelForNotification()
                ]
            };

            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResult);
            _mockUserService.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id="test123"});
            _mockUserService.Setup(x => x.GetActiveNetworkManagersByRpUserIdAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<List<User>>());

            // Act
            var result = await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ImportResult>(okResult.Value);

            Assert.Empty(response.DataForExistingOrgOrUser);
            Assert.Empty(response.DataForNewOrgOrUser);
        }

        [Fact]
        public async Task UploadCsv_Returns499_WhenOperationCancelled()
        {
            // Arrange
            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(StatusCodes.Status499ClientClosedRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task UploadCsv_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _mockCsvImportService
                .Setup(x => x.ImportFromCsvAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Import failed"));

            // Act
            var result = await _controller.UploadCsv("csv-content", CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        }
    }
}
