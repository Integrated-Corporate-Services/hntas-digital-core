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
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;
        private readonly Mock<IInvitationService> _mockInvitationService;

        private readonly SOAController _controller;

        public SoaControllerTests()
        {
            _mockSoaService = new Mock<ISoaService>();
            _mockLogger = new Mock<ILogger<SOAController>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockUserService = new Mock<IUserService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _controller = new SOAController(_mockSoaService.Object, _mockLogger.Object, _mockEmailService.Object, _mockHeatNetworkService.Object, _mockUserService.Object, _mockAuditService.Object, _mockNotificationHistoryService.Object, _mockInvitationService.Object);
        }

        [Fact]
        public async Task UpdateSoaStatus_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ElementSoaStatusUpdateRequest
            {
                HnId = "HN0000001",
                ElementId = "00001",
                Stage = SoaStage.Stage1,
                ElementSoaStatus = NetworkDetailsStatus.InProgress,
                SoaStatusUpdatedBy = "testuser",
                SoaStatuses = new List<SoaStatusWithCount>(),
                ElementType = ElementTypeInShort.EC
            };

            _mockSoaService
                .Setup(s => s.UpdateSoaStatus(
                    It.IsAny<string>(),
                    It.IsAny<ElementTypeInShort>(),                    
                    It.IsAny<SoaStage>(),
                    It.IsAny<List<SoaStatusWithCount>>(),
                    It.IsAny<string>(),
                    It.IsAny<NetworkDetailsStatus>()))
                .Returns(Task.CompletedTask);

            _mockHeatNetworkService.Setup(s => s.GetByHnIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new  HeatNetwork { HnId = "HN0000001", Name = "Test Heat Network" });

            // Act
            var result = await _controller.UpdateSoaStatus(request);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
        }
    }
}