using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class FeedbackControllerTests
    {
        private readonly Mock<IFeedbackService> _feedbackServiceMock;
        private readonly FeedbackController _controller;

        public FeedbackControllerTests()
        {
            _feedbackServiceMock = new Mock<IFeedbackService>();
            _controller = new FeedbackController(_feedbackServiceMock.Object);
        }

        [Fact]
        public async Task Create_Calls_Service_And_Returns_Ok()
        {
            // Arrange
            var controller = new FeedbackController(_feedbackServiceMock.Object);
            var request = new CreateFeedbackRequest();

            _feedbackServiceMock
                .Setup(x => x.CreateAsync(request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.Create(request);

            // Assert
            Assert.IsType<OkResult>(result);

            _feedbackServiceMock.Verify(
                x => x.CreateAsync(request),
                Times.Once);
        }
    }
}
