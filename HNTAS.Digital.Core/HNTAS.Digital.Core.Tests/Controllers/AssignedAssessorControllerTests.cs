using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.AssignedAssessor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class AssignedAssessorControllerTests
    {

        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<ILogger<AssignedAssessorController>> _mockLogger;
        private readonly AssignedAssessorController _controller;


        public AssignedAssessorControllerTests()
        {
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockLogger = new Mock<ILogger<AssignedAssessorController>>();

            _controller = new AssignedAssessorController(
                _mockLogger.Object,
                _mockHeatNetworkService.Object);
        }

        [Fact]
        public async Task GetAssignedAssessors_WithValidResult_ReturnsOk()
        {
            // Arrange
            var request = new AssignedAssessorRequest
            {
                Page = 1,
                PageSize = 10
            };

            var response = new AssignedAssessorResponse
            {
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1,
                Items = new List<AssignedAssessor>
                {
                    new AssignedAssessor
                    {
                        Name = "Test Assessor",
                        Email = "assessor@test.com",
                        HeatNetworkName = "HN-001",
                        ElementsAssigned = "EC-01",
                        Status = UserStatus.Active
                    }
                }
            };

            _mockHeatNetworkService
                .Setup(s => s.GetAssignedAssessors(It.IsAny<AssignedAssessorRequest>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.GetAssignedAssessors(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<AssignedAssessorResponse>(okResult.Value);

            Assert.Single(value.Items);
            Assert.Equal("Test Assessor", value.Items[0].Name);
        }

        [Fact]
        public async Task GetAssignedAssessors_WhenNoAssessorsFound_ReturnsNotFound()
        {
            // Arrange
            var request = new AssignedAssessorRequest
            {
                Page = 1,
                PageSize = 10
            };

            _mockHeatNetworkService
                .Setup(s => s.GetAssignedAssessors(It.IsAny<AssignedAssessorRequest>()))
                .ReturnsAsync((AssignedAssessorResponse)null);

            // Act
            var result = await _controller.GetAssignedAssessors(request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetAssignedAssessors_ThrowsException_WhenServiceFails()
        {
            // Arrange
            var request = new AssignedAssessorRequest
            {
                Page = 1,
                PageSize = 10
            };

            _mockHeatNetworkService
                .Setup(s => s.GetAssignedAssessors(It.IsAny<AssignedAssessorRequest>()))
                .ThrowsAsync(new Exception("Database failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _controller.GetAssignedAssessors(request));
        }


    }
}
