using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.NotificationHistory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class NotificationHistoryControllerTests
    {
        private readonly Mock<ILogger<NotificationHistoryController>> _mockLogger;
        private readonly Mock<INotificationHistoryService> _mockNotificationSevice;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserStatsService> _mockUserStatsService;
        private readonly NotificationHistoryController _controller;

        public NotificationHistoryControllerTests()
        {
            _mockLogger = new Mock<ILogger<NotificationHistoryController>>();
            _mockNotificationSevice = new Mock<INotificationHistoryService>();
            _mockUserService = new Mock<IUserService>();
            _mockUserStatsService = new Mock<IUserStatsService>();
            _controller = new NotificationHistoryController(_mockLogger.Object, _mockNotificationSevice.Object, _mockUserService.Object, _mockUserStatsService.Object);

        }

        [Fact]
        public async Task GetNotificationHistory_Ok()
        {
            var request = new NotificationHistoryRequest() { UserId = "test" };
            _mockNotificationSevice.Setup(n => n.GetNotificationHistory(It.IsAny<NotificationHistoryRequest>()))
                .ReturnsAsync(new NotificationHistoryResponse());

            _mockNotificationSevice.Setup(n => n.GetNotificationHistoryCount(It.IsAny<string>()))
                .ReturnsAsync(2);

            _mockUserStatsService.Setup(u => u.UpdateNotificationHistoryCountAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.GetNotificationHistory(request);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetNotificationHistory_HistoryNotFound()
        {
            var request = new NotificationHistoryRequest() { UserId = "test" };
            _mockNotificationSevice.Setup(n => n.GetNotificationHistory(It.IsAny<NotificationHistoryRequest>()))
                .ReturnsAsync((NotificationHistoryResponse)null!);            

            var result = await _controller.GetNotificationHistory(request);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetNotificationHistory_ThrowException()
        {
            var request = new NotificationHistoryRequest() { UserId = "test" };
            _mockNotificationSevice.Setup(n => n.GetNotificationHistory(It.IsAny<NotificationHistoryRequest>()))
                .Throws(new Exception());

            var result = await _controller.GetNotificationHistory(request);

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }

        [Fact]
        public async Task UnreadNotificationCount_Ok()
        {
            var request = new NotificationHistoryRequest() { UserId = "test" };
            _mockNotificationSevice.Setup(n => n.GetNotificationHistory(It.IsAny<NotificationHistoryRequest>()))
                .ReturnsAsync(new NotificationHistoryResponse());

            _mockNotificationSevice.Setup(n => n.GetNotificationHistoryCount(It.IsAny<string>()))
                .ReturnsAsync(4);

            _mockUserStatsService.Setup(u => u.GetNotificationHistoryCountAsync(It.IsAny<string>()))
                .ReturnsAsync(2);

            var result = await _controller.UnreadNotificationCount("userId", HNTAS.Core.Api.Enums.UserRole.NetworkManager);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UnreadNotificationCount_ThrowException()
        {
            var request = new NotificationHistoryRequest() { UserId = "test" };
            _mockNotificationSevice.Setup(n => n.GetNotificationHistory(It.IsAny<NotificationHistoryRequest>()))
                .ReturnsAsync(new NotificationHistoryResponse());

            _mockNotificationSevice.Setup(n => n.GetNotificationHistoryCount(It.IsAny<string>()))
                .Throws(new Exception());            

            var result = await _controller.UnreadNotificationCount("userId", HNTAS.Core.Api.Enums.UserRole.NetworkManager);

            var res = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, res.StatusCode);
        }
    }
}
