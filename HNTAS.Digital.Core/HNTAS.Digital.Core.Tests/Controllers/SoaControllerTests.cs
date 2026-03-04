using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class SoaControllerTests
    {
        private readonly Mock<ISoaService> _mockSoaService;
        private readonly Mock<ILogger<SOAController>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IUserService> _mockUserService;

        private readonly SOAController _controller;

        public SoaControllerTests()
        {
            _mockSoaService = new Mock<ISoaService>();
            _mockLogger = new Mock<ILogger<SOAController>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockUserService = new Mock<IUserService>();
            _controller = new SOAController(_mockSoaService.Object, _mockLogger.Object, _mockEmailService.Object, _mockHeatNetworkService.Object, _mockUserService.Object);
        }

        [Fact]
        public async Task SaveSoaDocument_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ElementSoaUploadDocumentRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1, 
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            _mockSoaService
                .Setup(s => s.UpdateSoaDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<NetworkDetailsUploadedDocument>(),
                    It.IsAny<string>(),
                    It.IsAny<SoaStage>(),
                    It.IsAny<NetworkDetailsStatus>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SaveSoaDocument(request);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
        }                     
    }
}