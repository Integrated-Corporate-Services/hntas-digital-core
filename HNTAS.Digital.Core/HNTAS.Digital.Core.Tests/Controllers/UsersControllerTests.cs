using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IOrganisationService> _mockOrgService;
        private readonly Mock<IInvitationService> _mockInvitationService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly Mock<ICounterService> _mockCounterService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly UsersController _controller;
        private readonly Mock<IHeatNetworkService> _mockHeatNetworkService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<INotificationHistoryService> _mockNotificationHistoryService;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockOrgService = new Mock<IOrganisationService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _mockCounterService = new Mock<ICounterService>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailService = new Mock<IEmailService>();
            _mockHeatNetworkService = new Mock<IHeatNetworkService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockNotificationHistoryService = new Mock<INotificationHistoryService>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockOrgService.Object,
                _mockInvitationService.Object,
                _mockLogger.Object,
                _mockCounterService.Object,
                _mockMapper.Object,
                _mockEmailService.Object,
                _mockHeatNetworkService.Object,
                _mockAuditService.Object,
                _mockNotificationHistoryService.Object
            );
        }

        #region GetUsersDetails Tests

        [Fact]
        public async Task GetUsersDetails_ReturnsOk_WithValidUser()
        {
            // Arrange
            string userId = "user-abc";

            // Creating the result object expected by the service
            var mockDetailsResult = new UserDetailsResult
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe"
                // Add other properties required by your UserDetailsResult model
            };

            var mockResponse = new UserDetailsResponse
            {
                Id = userId,
                FirstName = "John"
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync(mockDetailsResult);

            _mockMapper.Setup(m => m.Map<UserDetailsResponse>(mockDetailsResult))
                .Returns(mockResponse);

            // Act
            var result = await _controller.GetUsersDetails(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDetailsResponse>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);

            // Verify Logger was called (Information)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully retrieved")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetUsersDetails_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            string userId = "user-abc";
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ThrowsAsync(new System.Exception("Critical Failure"));

            // Act
            var result = await _controller.GetUsersDetails(userId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("An unexpected error occurred while retrieving users.", objectResult.Value);

            // Verify Error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while retrieving all users")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_ReturnsOk_WhenUserExists()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1"; // 24-character hex string
            var mockUser = new User { Id = userId, FirstName = "Alice" };
            var mockResponse = new UserResponse { Id = userId, FirstName = "Alice" };

            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync(mockUser);

            _mockMapper.Setup(m => m.Map<UserResponse>(mockUser))
                .Returns(mockResponse);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserResponse>(actionResult.Value);
            Assert.Equal(userId, returnedUser.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1";
            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_ReturnsInternalServerError_OnException()
        {
            // Arrange
            string userId = "65f1a2b3c4d5e6f7a8b9c0d1";
            _mockUserService.Setup(s => s.GetByIdAsync(userId))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.GetById(userId);

            // Assert
            var actionResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, actionResult.StatusCode);
            Assert.Equal("An unexpected error occurred while validating the user ID.", actionResult.Value);
        }

        #endregion

        #region GetManagedUsersAsync Tests

        [Fact]
        public async Task GetManagedUsersAsync_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string userId = "user-123";
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync((UserDetailsResult)null);

            // Act
            var result = await _controller.GetManagedUsersAsync(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetManagedUsersAsync_ReturnsOk_WithCombinedManagedUsers()
        {
            // Arrange
            string userId = "resp-user-id";
            var mainUser = new UserDetailsResult { Id = userId, EmailId = "resp@test.com" };

            var invitedUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult { Id = "reg-user-id", EmailId = "registered@test.com" }
            };

            var invitations = new List<ManagedUserResponse>
            {
                new ManagedUserResponse { EmailId = "invited@test.com", Status = "Invited" }
            };

            // Mock Main User Retrieval
            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId))
                .ReturnsAsync(mainUser);

            _mockMapper.Setup(m => m.Map<ManagedUserResponse>(mainUser))
                .Returns(new ManagedUserResponse { Id = userId, EmailId = "resp@test.com" });

            // Mock Invitations
            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(userId))
                .ReturnsAsync(invitations);

            // Mock Registered Users from Invitations
            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(invitedUsersDetail);

            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(invitedUsersDetail))
                .Returns(new List<ManagedUserResponse>
                {
            new ManagedUserResponse { Id = "reg-user-id", EmailId = "registered@test.com" }
                });

            // Act
            var result = await _controller.GetManagedUsersAsync(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // Should contain: 1 Registered + 1 Invited
            Assert.Equal(2, managedUsers.Count);
            //Assert.Contains(managedUsers, u => u.EmailId == "resp@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "registered@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "invited@test.com");
        }

        [Fact]
        public async Task GetManagedUsersAsync_ExcludesResponsibleUserFromFinalResult()
        {
            // Arrange
            string rpUserId = "resp-user-id";
            string contributorEmail = "contributor@test.com";

            var rpUser = new UserDetailsResult { Id = rpUserId, EmailId = "rp@test.com" };

            // Registered users list contains the RP AND a contributor
            var invitedUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult { Id = rpUserId, EmailId = "rp@test.com" }, // RP
                new UserDetailsResult { Id = "other-id", EmailId = contributorEmail } // Contributor
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(rpUserId)).ReturnsAsync(rpUser);

            // Mocking the list return
            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(invitedUsersDetail))
                .Returns(new List<ManagedUserResponse>
                {
                    new ManagedUserResponse { Id = rpUserId, EmailId = "rp@test.com" },
                    new ManagedUserResponse { Id = "other-id", EmailId = contributorEmail }
                });

            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(rpUserId))
                .ReturnsAsync(new List<ManagedUserResponse>());

            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(invitedUsersDetail);

            // Act
            var result = await _controller.GetManagedUsersAsync(rpUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // Should only have 1 entry (the contributor) because the RP (resp-user-id) is excluded
            Assert.Single(managedUsers);
            Assert.All(managedUsers, u => Assert.NotEqual(rpUserId, u.Id));
            Assert.Contains(managedUsers, u => u.EmailId == contributorEmail);
        }


        [Fact]
        public async Task GetManagedUsersAsync_HandlesRejectedAndRegisteredLogic_Correctly()
        {
            // Arrange
            string rpUserId = "rp-id";
            string networkId = "network-A";
            string email1 = "contributor1@test.com";
            string email2 = "contributor2@test.com";

            var rpUser = new UserDetailsResult { Id = rpUserId, FirstName = "rp", LastName = "user", Roles = new List<UserRole> { UserRole.ResponsiblePerson }, Status = UserStatus.Active, EmailId = "rpuser@test.com" };

            // 1. Registered Users (User 1)
            var registeredUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult
                {
                    Id = "u1-reg-id", // Ensure this ID matches what the loop expects
                    FirstName = "User",
                    LastName = "One",
                    EmailId = email1,
                    Status = UserStatus.Active,
                    Roles = new List<UserRole> { UserRole.Contributor },
                    HnRoleMappings = new List<HnRoleMappingsUserResult> { new HnRoleMappingsUserResult { HeatNetwork = new HeatNetworkUserResponse { HnId = networkId }, Role = "DesignatedDesigner" } }
                }
            };

            // 2. Invitations
            var invitations = new List<ManagedUserResponse>
            {
                new ManagedUserResponse { EmailId = email1, Status = "Rejected", InvitedAt = DateTime.Now.AddDays(-5),
                    HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
                new ManagedUserResponse { EmailId = email1, Status = "Invited", InvitedAt = DateTime.Now.AddDays(-2),
                     HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
                new ManagedUserResponse { EmailId = email2, Status = "Rejected", InvitedAt = DateTime.Now.AddDays(-1),
                    HeatNetworks = new List<HeatNetworkInfo> { new HeatNetworkInfo { HnId = networkId } } },
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(rpUserId)).ReturnsAsync(rpUser);
            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(rpUserId)).ReturnsAsync(invitations);
            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>())).ReturnsAsync(registeredUsersDetail);

            // FIX: Ensure the mapped object has the ID so FirstOrDefault(x => x.Id == ruser.Id) works
            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(registeredUsersDetail))
                .Returns(new List<ManagedUserResponse> {
            new ManagedUserResponse {
                Id = "u1-reg-id", // MUST MATCH the Detail ID above
                EmailId = email1,
                Status = UserStatus.Active.ToString()
            }
                });

            // Act
            var result = await _controller.GetManagedUsersAsync(rpUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // We expect 2 contributors. RP is filtered out by the controller logic.
            Assert.Equal(2, managedUsers.Count);

            // Verify User 1: Registered record exists, invite record is hidden
            Assert.Contains(managedUsers, u => u.EmailId == email1 && u.Status == UserStatus.Active.ToString());
            Assert.DoesNotContain(managedUsers, u => u.EmailId == email1 && u.Status == "Rejected");

            // Verify User 2: Still shows "Rejected" because they are not registered
            Assert.Contains(managedUsers, u => u.EmailId == email2 && u.Status == "Rejected");

            // Verify RP is NOT in the list
            Assert.DoesNotContain(managedUsers, u => u.EmailId == "rpuser@test.com");
        }

        #endregion

        #region IsRpUser

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsRpUser_ShouldReturnOk_WithCorrectRoleStatus(bool hasRole)
        {
            // Arrange
            string email = "test@example.com";
            var user = new User
            {
                EmailId = email,
                Roles = hasRole ? new List<UserRole> { UserRole.ResponsiblePerson } : new List<UserRole>()
            };

            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(hasRole, okResult.Value);
        }


        [Fact]
        public async Task IsRpUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "test@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task IsRpUser_ShouldReturn500_WhenExceptionOccurs()
        {
            // Arrange
            string email = "error@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ThrowsAsync(new System.Exception("Database failure"));

            // Act
            var result = await _controller.IsRpUser(email);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }
        #endregion

        #region IsActiveUser
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task IsActiveUser_ShouldReturnBadRequest_WhenEmailIsInvalid(string email)
        {
            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Email ID must be provided.", badRequestResult.Value);
        }

        [Theory]
        [InlineData(UserStatus.Active, true)]
        [InlineData(UserStatus.InActive, false)]
        public async Task IsActiveUser_ShouldReturnOk_WithExpectedActiveStatus(UserStatus status, bool expectedResult)
        {
            // Arrange
            string email = "test@example.com";
            var user = new User { EmailId = email, Status = status };

            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task IsActiveUser_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "unknown@example.com";
            _mockUserService.Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.IsActiveUser(email);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
        #endregion
                
        #region CheckOrganisationExistence
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CheckOrganisationExistence_ShouldReturnOk_WhenServiceReturnsResult(bool serviceResult)
        {
            // Arrange
            string houseNumber = "12345678";
            _mockOrgService.Setup(s => s.IsOrganizationExists(houseNumber))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CheckOrganisationExistence(houseNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(serviceResult, okResult.Value);
        }

        [Fact]
        public async Task CheckOrganisationExistence_ShouldReturn500_WhenExceptionOccurs()
        {
            // Arrange
            string houseNumber = "12345678";
            _mockOrgService.Setup(s => s.IsOrganizationExists(houseNumber))
                .ThrowsAsync(new System.Exception("Database connection failed"));

            // Act
            var result = await _controller.CheckOrganisationExistence(houseNumber);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("An unexpected error occurred.", objectResult.Value);
        }
        #endregion

        #region GetHeatNetworkUsersWithRoles

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnBadRequest_WhenHnIdIsMissing(string invalidId)
        {
            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(invalidId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Heat Network ID must be provided.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnOk_WithRpAtFirstPosition()
        {
            // Arrange
            var hnId = "HN123";
            var rpUser = new User { Id = "rp-user-id", EmailId = "rp@test.com" };
            var otherUsers = new List<UserRoleDetailResponse>
        {
            new() { EmailId = "other@test.com" }
        };
            var mappedRp = new UserRoleDetailResponse { EmailId = "rp@test.com" };

            _mockUserService.Setup(s => s.GetResponsiblePersonByHnIdAsync(hnId))
                .ReturnsAsync(rpUser);
            _mockUserService.Setup(s => s.GetHeatNetworkUsersWithRolesAsync(hnId))
                .ReturnsAsync(otherUsers);
            _mockMapper.Setup(m => m.Map<UserRoleDetailResponse>(rpUser))
                .Returns(mappedRp);

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var finalData = Assert.IsType<List<UserRoleDetailResponse>>(okResult.Value);

            Assert.Equal(2, finalData.Count);
            Assert.Equal("rp@test.com", finalData[0].EmailId); // Verify RP is at index 0
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldWork_WhenNoOtherUsersFound()
        {
            // Arrange
            var hnId = "HN123";
            _mockUserService.Setup(s => s.GetResponsiblePersonByHnIdAsync(hnId))
                .ReturnsAsync(new User());
            _mockUserService.Setup(s => s.GetHeatNetworkUsersWithRolesAsync(hnId))
                .ReturnsAsync((List<UserRoleDetailResponse>)null); // Service returns null
            _mockMapper.Setup(m => m.Map<UserRoleDetailResponse>(It.IsAny<User>()))
                .Returns(new UserRoleDetailResponse());

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var finalData = Assert.IsType<List<UserRoleDetailResponse>>(okResult.Value);
            Assert.Single(finalData); // Only the RP should be present
        }

        [Fact]
        public async Task GetHeatNetworkUsersWithRoles_ShouldReturnNotFound_WhenNoRpExists()
        {
            // Arrange
            var hnId = "HN123";
            _mockUserService.Setup(s => s.GetResponsiblePersonByHnIdAsync(hnId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetHeatNetworkUsersWithRoles(hnId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("No Responsible Person found", notFoundResult.Value.ToString());
        }
        #endregion

        #region UpdateOrgId

        [Fact]
        public async Task UpdateOrgId_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("UserId", "Required");
            var request = new UpdateUserOrgIdRequest();

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturnNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "user-123", OrgId = "org-456" };

            // 1. Mock the UpdateResult
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(1);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturnNotFound_WhenNoUserMatches()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "invalid-id", OrgId = "org-1" };

            // Fix: Mock the abstract UpdateResult class
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(true);
            mockUpdateResult.Setup(r => r.MatchedCount).Returns(0);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"User with ID '{request.UserId}' not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateOrgId_ShouldReturn500_WhenDatabaseNotAcknowledged()
        {
            // Arrange
            var request = new UpdateUserOrgIdRequest { UserId = "user-1", OrgId = "org-1" };

            // Fix: Mock the abstract UpdateResult class
            var mockUpdateResult = new Mock<UpdateResult>();
            mockUpdateResult.Setup(r => r.IsAcknowledged).Returns(false);

            _mockUserService.Setup(s => s.UpdateOrgIdAsync(request.UserId, request.OrgId))
                .ReturnsAsync(mockUpdateResult.Object);

            // Act
            var result = await _controller.UpdateOrgId(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            Assert.Equal("Database update operation was not acknowledged.", objectResult.Value);
        }
        #endregion
    }
}
