using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Assessor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class AssessorControllerTests
    {

        private readonly Mock<IAssessorService> _mockAssessorService;
        private readonly AssessorController _controller;

        public AssessorControllerTests()
        {
            _mockAssessorService = new Mock<IAssessorService>();
            _controller = new AssessorController(_mockAssessorService.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        public async Task Search_WithInvalidQuery_ReturnsEmptyList(string query)
        {
            // Act
            var result = await _controller.Search(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<List<AssessorSearchResult>>(okResult.Value);
            Assert.Empty(value);

            _mockAssessorService.Verify(
                s => s.GetAssessorSuggestionsAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Search_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var results = new List<AssessorSearchResult>
            {
                new AssessorSearchResult { Id = "A1", FirstName = "Test" , LastName = "Assessor", Email = "TestAssessor@ex.com"}
            };

            _mockAssessorService
                .Setup(s => s.GetAssessorSuggestionsAsync("te"))
                .ReturnsAsync(results);

            // Act
            var result = await _controller.Search("te");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<List<AssessorSearchResult>>(okResult.Value);

            Assert.Single(value);
            Assert.Equal("A1", value[0].Id);
        }

        [Fact]
        public async Task Search_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _mockAssessorService
                .Setup(s => s.GetAssessorSuggestionsAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB failure"));

            // Act
            var result = await _controller.Search("te");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("Internal server error during search", objectResult.Value);
        }
    }
}
