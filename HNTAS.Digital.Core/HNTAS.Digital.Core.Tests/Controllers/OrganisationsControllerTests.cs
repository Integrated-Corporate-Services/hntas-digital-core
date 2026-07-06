using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class OrganisationsControllerTests
    {

        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IOrganisationService> _mockOrgService;
        private readonly Mock<ILogger<OrganisationsController>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly OrganisationsController _controller;


        public OrganisationsControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockOrgService = new Mock<IOrganisationService>();
            _mockLogger = new Mock<ILogger<OrganisationsController>>();
            _mockMapper = new Mock<IMapper>();

            _controller = new OrganisationsController(
                _mockOrgService.Object,
                _mockUserService.Object,
                _mockEmailService.Object,
                _mockLogger.Object,
                _mockMapper.Object
            );
        }

        #region  EditOrgDetails Tests

        [Fact]
        public async Task EditOrgDetails_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "invalid-user";
            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.EditOrgDetails("org123", userId, new OrganisationRequest());

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"User with ID: {userId} not found.", actionResult.Value);
        }

        [Fact]
        public async Task EditOrgDetails_ReturnsNoContent_OnSuccess()
        {
            // Arrange
            var userId = "user1";
            var orgId = "org1";
            var request = new OrganisationRequest { Name = "New Org", RegisteredAddress = new RegisteredAddress() };

            _mockUserService.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(new User { FirstName = "John", LastName = "Doe" });
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync(new Organisation { Id = "db-id", Name = "Old Org" });

            // Act
            var result = await _controller.EditOrgDetails(orgId, userId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockOrgService.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<Organisation>()), Times.Once);
        }

        [Fact]
        public async Task ExistsByDetails_ReturnsBadRequest_WhenParamsMissing()
        {
            // Act
            var result = await _controller.ExistsByDetails(null, "", "UK");

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Name, postcode, and country are required parameters.", actionResult.Value);
        }

        #endregion

        #region ExistsByDetails Tests

        [Fact]
        public async Task ExistsByDetails_ReturnsBadRequest_WhenParametersAreMissing()
        {
            // Arrange & Act
            // Testing with one parameter missing (name)
            var result = await _controller.ExistsByDetails(null, "LS1 1UR", "UK");

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Name, postcode, and country are required parameters.", actionResult.Value);
        }

        [Fact]
        public async Task ExistsByDetails_ReturnsOkTrue_WhenOrganisationExists()
        {
            // Arrange
            string name = "Test Org";
            string postCode = "LS1 1UR";
            string country = "UK";

            _mockOrgService.Setup(s => s.ExistsByDetailsAsync(name, postCode, country))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ExistsByDetails(name, postCode, country);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            bool exists = Assert.IsType<bool>(okResult.Value);
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsByDetails_ReturnsOkFalse_WhenOrganisationDoesNotExist()
        {
            // Arrange
            string name = "Non Existent Org";
            string postCode = "XX1 1XX";
            string country = "UK";

            _mockOrgService.Setup(s => s.ExistsByDetailsAsync(name, postCode, country))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ExistsByDetails(name, postCode, country);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            bool exists = Assert.IsType<bool>(okResult.Value);
            Assert.False(exists);
        }

        #endregion

        #region GetByOrgId Tests

        [Fact]
        public async Task GetByOrgId_ReturnsOk_WhenOrgFound()
        {
            // Arrange
            var orgId = "ORG123";
            var mockOrg = new Organisation { Name = "Found Org" };
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync(mockOrg);

            // Act
            var result = await _controller.GetByOrgId(orgId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrg = Assert.IsType<Organisation>(actionResult.Value);
            Assert.Equal("Found Org", returnedOrg.Name);
        }

        #endregion

        #region UpdateHeatNetworkId Tests

        [Fact]
        public async Task UpdateHeatNetworkId_ReturnsNotFound_WhenOrgMissing()
        {
            // Arrange
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(It.IsAny<string>())).ReturnsAsync((Organisation)null);

            // Act
            var result = await _controller.UpdateHeatNetworkId("MISSING", "user1", "hn1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateHeatNetworkId_ReturnsInternalError_OnException()
        {
            // Arrange
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("Database failure"));

            // Act
            var result = await _controller.UpdateHeatNetworkId("ORG1", "user1", "hn1");

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, actionResult.StatusCode);
            Assert.Equal("An unexpected error occurred while updating the heat network ID.", actionResult.Value);
        }

        [Fact]
        public async Task UpdateHeatNetworkId_ReturnsNotFound_WhenOrganisationDoesNotExist()
        {
            // Arrange
            _mockOrgService
                .Setup(s => s.GetByOrgIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Organisation)null);

            // Act
            var result = await _controller.UpdateHeatNetworkId("ORG1", "user1", "hn1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateHeatNetworkId_AddsHeatNetworkId_WhenNotAlreadyPresent()
        {
            // Arrange
            var organisation = new Organisation
            {
                Id = "ORG_INTERNAL_ID",
                HnIds = new List<string>()
            };

            _mockOrgService
                .Setup(s => s.GetByOrgIdAsync(It.IsAny<string>()))
                .ReturnsAsync(organisation);

            _mockOrgService
                .Setup(s => s.UpdateAsync(
                    organisation.Id,
                    It.IsAny<Organisation>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateHeatNetworkId("ORG1", "user1", "hn1");

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Contains("hn1", organisation.HnIds);
            Assert.Equal("user1", organisation.LastModifiedBy);
            Assert.True(organisation.LastModifiedAt <= DateTime.UtcNow);

            _mockOrgService.Verify(s =>
                s.UpdateAsync(
                    organisation.Id,
                    It.IsAny<Organisation>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateHeatNetworkId_DoesNotUpdate_WhenHeatNetworkIdAlreadyExists()
        {
            // Arrange
            var organisation = new Organisation
            {
                Id = "ORG_INTERNAL_ID",
                HnIds = new List<string> { "hn1" }
            };

            _mockOrgService
                .Setup(s => s.GetByOrgIdAsync(It.IsAny<string>()))
                .ReturnsAsync(organisation);

            // Act
            var result = await _controller.UpdateHeatNetworkId("ORG1", "user1", "hn1");

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockOrgService.Verify(s =>
                s.UpdateAsync(It.IsAny<string>(), It.IsAny<Organisation>()),
                Times.Never);
        }


        #endregion

        #region GetByOrgIdOrName Tests

        [Fact]
        public async Task GetByOrgIdOrName_ReturnsBadRequest_WhenTermIsEmpty()
        {
            // Arrange
            string term = "   "; // Testing whitespace

            // Act
            var result = await _controller.GetByOrgIdOrName(term);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search term cannot be empty.", actionResult.Value);
        }

        [Fact]
        public async Task GetByOrgIdOrName_ReturnsNotFound_WhenNoOrganisationExists()
        {
            // Arrange
            string term = "UnknownOrg";
            _mockOrgService.Setup(s => s.GetByOrgIdOrNameAsync(term))
                .ReturnsAsync((Organisation)null);

            // Act
            var result = await _controller.GetByOrgIdOrName(term);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No organisation found for search term: '{term}'", actionResult.Value);
        }

        [Fact]
        public async Task GetByOrgIdOrName_ReturnsOk_WhenOrganisationIsFound()
        {
            // Arrange
            string term = "OrgName123";
            var expectedOrg = new Organisation
            {
                Id = "db-123",
                Name = "OrgName123",
                OrgId = "ORG-123"
            };

            _mockOrgService.Setup(s => s.GetByOrgIdOrNameAsync(term))
                .ReturnsAsync(expectedOrg);

            // Act
            var result = await _controller.GetByOrgIdOrName(term);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrg = Assert.IsType<Organisation>(actionResult.Value);
            Assert.Equal(expectedOrg.Name, returnedOrg.Name);
            Assert.Equal(expectedOrg.OrgId, returnedOrg.OrgId);
        }

        #endregion

    }
}
