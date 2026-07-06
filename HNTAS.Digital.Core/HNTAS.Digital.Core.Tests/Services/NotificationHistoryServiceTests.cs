using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models.NotificationHistory;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class NotificationHistoryServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<NotificationHistory>> _mockCollection;
        private readonly Mock<ILogger<NotificationHistoryService>> _mockLogger;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockSettings;
        private readonly NotificationHistoryService _sut;
        public NotificationHistoryServiceTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<NotificationHistory>>();
            _mockLogger = new Mock<ILogger<NotificationHistoryService>>();
            _mockSettings = new Mock<IOptions<AWSDocDbSettings>>();

            var settings = new AWSDocDbSettings
            {
                HeatNetworksCollectionName = "NotificationHistory"
            };

            _mockSettings.Setup(s => s.Value).Returns(settings);
            // Setup the mock database to return the mock collection
            _mockDatabase.Setup(db => db.GetCollection<NotificationHistory>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);

            _sut = new NotificationHistoryService(_mockSettings.Object, _mockDatabase.Object, _mockLogger.Object);
        }


        [Fact]
        public async Task CreateAsync_ShouldInsertNotificationHistory()
        {
            // Arrange
            var notificationHistory = new NotificationHistory
            {
                Id = Guid.NewGuid().ToString(),
                NotificationType = HNTAS.Core.Api.Enums.NotificationHistoryType.ContributorAcceptsInviteToHeatNetwork,
                ActorsId = new List<string> { "User1" },
                Subject = "Test Subject",                
                Timestamp = DateTime.UtcNow
            };
            // Act
            await _sut.CreateAsync(notificationHistory);
            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(notificationHistory, null, default), Times.Once);
        }

        [Fact]
        public async Task GetNotificationHistory_ShouldReturnNotificationHistoryResponse()
        {
            // Arrange
            var request = new NotificationHistoryRequest
            {
                UserId = "User1",
                Page = 1,
                PageSize = 10,
                SortBy = "timestamp",
                SortDirection = "desc"
            };
            var notificationHistories = new List<NotificationHistory>
            {
                new NotificationHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    NotificationType = HNTAS.Core.Api.Enums.NotificationHistoryType.ContributorAcceptsInviteToHeatNetwork,
                    ActorsId = new List<string> { "User1" },
                    Subject = "Test Subject",
                    Timestamp = DateTime.UtcNow
                }
            };            

            var mockCursor = new Mock<IAsyncCursor<NotificationHistory>>();
            mockCursor.Setup(c => c.Current).Returns(notificationHistories);
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            // Setup mock for FindAsync
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<NotificationHistory>>(),
                It.IsAny<FindOptions<NotificationHistory, NotificationHistory>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _sut.GetNotificationHistory(request);
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetNotificationHistory_ShouldThrowException_WhenDatabaseErrorOccurs()
        {
            // Arrange
            var request = new NotificationHistoryRequest
            {
                UserId = "User1",
                Page = 1,
                PageSize = 10,
                SortBy = "timestamp",
                SortDirection = "desc"
            };
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<NotificationHistory>>(),
                It.IsAny<FindOptions<NotificationHistory, NotificationHistory>>(),
                default))
                .ThrowsAsync(new Exception("Database error"));
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.GetNotificationHistory(request));
        }

        [Fact]
        public async Task GetNotificationHistoryCount_ShouldReturnCount()
        {
            // Arrange
            var request = new NotificationHistoryRequest
            {
                UserId = "User1",
                Page = 1,
                PageSize = 10,
                SortBy = "timestamp",
                SortDirection = "desc"
            };
            _mockCollection.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<NotificationHistory>>(),
                null,
                default))
                .ReturnsAsync(5);
            // Act
            var result = await _sut.GetNotificationHistoryCount("test@gmail.com");
            // Assert
            Assert.Equal(5, result);
        }
    }
}
