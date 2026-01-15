using AutoMapper;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockOrgService = new Mock<IOrganisationService>();
            _mockInvitationService = new Mock<IInvitationService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _mockCounterService = new Mock<ICounterService>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailService = new Mock<IEmailService>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockOrgService.Object,
                _mockInvitationService.Object,
                _mockLogger.Object,
                _mockCounterService.Object,
                _mockMapper.Object,
                _mockEmailService.Object
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

            // Should contain: 1 Responsible + 1 Registered + 1 Invited
            Assert.Equal(3, managedUsers.Count);
            Assert.Contains(managedUsers, u => u.EmailId == "resp@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "registered@test.com");
            Assert.Contains(managedUsers, u => u.EmailId == "invited@test.com");
        }

        [Fact]
        public async Task GetManagedUsersAsync_ExcludesResponsibleUserFromRegisteredList()
        {
            // Arrange
            string userId = "resp-user-id";
            string email = "same@test.com";
            var mainUser = new UserDetailsResult { Id = userId, EmailId = email };

            // Registered users list contains the same user (edge case)
            var invitedUsersDetail = new List<UserDetailsResult>
            {
                new UserDetailsResult { Id = userId, EmailId = email }
            };

            _mockUserService.Setup(s => s.GetUserWithDetailsAsync(userId)).ReturnsAsync(mainUser);
            _mockMapper.Setup(m => m.Map<ManagedUserResponse>(mainUser))
                .Returns(new ManagedUserResponse { Id = userId, EmailId = email });

            _mockInvitationService.Setup(s => s.GetInvitedUsersAsRegisteredAsync(userId))
                .ReturnsAsync(new List<ManagedUserResponse>());

            _mockUserService.Setup(s => s.GetUsersByInvitedEmailsWithDetailsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(invitedUsersDetail);

            _mockMapper.Setup(m => m.Map<List<ManagedUserResponse>>(invitedUsersDetail))
                .Returns(new List<ManagedUserResponse> { new ManagedUserResponse { Id = userId, EmailId = email } });

            // Act
            var result = await _controller.GetManagedUsersAsync(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var managedUsers = Assert.IsType<List<ManagedUserResponse>>(okResult.Value);

            // Should only have 1 entry because the duplicate email was filtered out
            Assert.Single(managedUsers);
        }

        #endregion
    }
}
