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
    public class OrganisationUserControllerTests
    {
        private readonly Mock<IOrganisationService> _mockOrgService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<OrganisationUserController>> _mockLogger;
        private readonly OrganisationUserController _controller;

        public OrganisationUserControllerTests()
        {
            _mockOrgService = new Mock<IOrganisationService>();
            _mockUserService = new Mock<IUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<OrganisationUserController>>();

            _controller = new OrganisationUserController(
                _mockOrgService.Object,
                _mockUserService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }


        #region GetResponsiblePartyDetails Tests

        [Fact]
        public async Task GetResponsiblePartyDetails_ReturnsBadRequest_WhenOrgIdIsEmpty()
        {
            // Arrange
            string orgId = " ";

            // Act
            var result = await _controller.GetResponsiblePartyDetails(orgId);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("OrgId must be provided.", actionResult.Value);
        }

        [Fact]
        public async Task GetResponsiblePartyDetails_ReturnsNotFound_WhenOrganisationDoesNotExist()
        {
            // Arrange
            string orgId = "ORG123";
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync((Organisation)null);

            // Act
            var result = await _controller.GetResponsiblePartyDetails(orgId);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Organisation with OrgId '{orgId}' not found.", actionResult.Value);
        }

        [Fact]
        public async Task GetResponsiblePartyDetails_ReturnsNotFound_WhenRpUserIdIsMissing()
        {
            // Arrange
            string orgId = "ORG123";
            var mockOrg = new Organisation { OrgId = orgId, RpUserId = null };
            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync(mockOrg);

            // Act
            var result = await _controller.GetResponsiblePartyDetails(orgId);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"No Responsible Party (RP) is assigned to Organisation '{orgId}'.", actionResult.Value);
        }

        [Fact]
        public async Task GetResponsiblePartyDetails_ReturnsInternalError_WhenUserLookupFails()
        {
            // Arrange
            string orgId = "ORG123";
            string rpUserId = "USER456";
            var mockOrg = new Organisation { OrgId = orgId, RpUserId = rpUserId };

            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync(mockOrg);
            _mockUserService.Setup(s => s.GetByIdAsync(rpUserId)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetResponsiblePartyDetails(orgId);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, actionResult.StatusCode);
            Assert.Equal($"Internal Error: RP User ID '{rpUserId}' found but user details could not be retrieved.", actionResult.Value);
        }

        [Fact]
        public async Task GetResponsiblePartyDetails_ReturnsOk_OnSuccess()
        {
            // Arrange
            string orgId = "ORG123";
            string rpUserId = "USER456";
            var mockOrg = new Organisation { OrgId = orgId, RpUserId = rpUserId };
            var mockUser = new User { Id = rpUserId, FirstName = "Jane" };
            var mockResponse = new UserResponse { FirstName = "Jane" };

            _mockOrgService.Setup(s => s.GetByOrgIdAsync(orgId)).ReturnsAsync(mockOrg);
            _mockUserService.Setup(s => s.GetByIdAsync(rpUserId)).ReturnsAsync(mockUser);
            _mockMapper.Setup(m => m.Map<UserResponse>(mockUser)).Returns(mockResponse);

            // Act
            var result = await _controller.GetResponsiblePartyDetails(orgId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserResponse>(actionResult.Value);
            Assert.Equal("Jane", returnedUser.FirstName);
        }

        #endregion
    }
}
