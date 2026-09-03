using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services;

public class UserStatsServiceTests
{
    private readonly Mock<IMongoCollection<UserStats>> _mockCollection;
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<ILogger<UserStatsService>> _mockLogger;
    private readonly Mock<IOptions<AWSDocDbSettings>> _mockOptions;
    private readonly UserStatsService _service;

    public UserStatsServiceTests()
    {
        _mockCollection = new Mock<IMongoCollection<UserStats>>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockLogger = new Mock<ILogger<UserStatsService>>();
        _mockOptions = new Mock<IOptions<AWSDocDbSettings>>();

        // Setup options mock
        var settings = new AWSDocDbSettings { UserStatsCollectionName = "user_stats" };
        _mockOptions.Setup(o => o.Value).Returns(settings);

        // Setup database mock to return the mock collection
        _mockDatabase
            .Setup(db => db.GetCollection<UserStats>(settings.UserStatsCollectionName, null))
            .Returns(_mockCollection.Object);

        // Initialize service
        _service = new UserStatsService(_mockOptions.Object, _mockDatabase.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateNotificationHistoryCountAsync_ShouldCallUpdateOneAsyncWithCorrectParameters()
    {
        // Arrange
        var userId = "user123";
        var count = 5;

        _mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserStats>>(),
                It.IsAny<UpdateDefinition<UserStats>>(),
                It.Is<UpdateOptions>(o => o.IsUpsert == true),
                default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _service.UpdateNotificationHistoryCountAsync(userId, count);

        // Assert
        _mockCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<UserStats>>(),
            It.IsAny<UpdateDefinition<UserStats>>(),
            It.Is<UpdateOptions>(o => o.IsUpsert == true),
            default), Times.Once);
    }

    [Fact]
    public async Task GetNotificationHistoryCountAsync_WhenUserExists_ShouldReturnCount()
    {
        // Arrange
        var userId = "user123";
        var expectedCount = 10;
        var userStats = new UserStats { UserId = userId, NotificationHistoryCount = expectedCount };

        // Mocking the async cursor for MongoDB Find operation
        var mockCursor = new Mock<IAsyncCursor<UserStats>>();
        mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        mockCursor.Setup(_ => _.Current).Returns(new List<UserStats> { userStats });

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<UserStats>>(),
                It.IsAny<FindOptions<UserStats, UserStats>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetNotificationHistoryCountAsync(userId);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetNotificationHistoryCountAsync_WhenUserDoesNotExist_ShouldReturnZero()
    {
        // Arrange
        var userId = "unknown_user";

        // Mocking an empty result cursor
        var mockCursor = new Mock<IAsyncCursor<UserStats>>();
        mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockCursor.Setup(_ => _.Current).Returns(new List<UserStats>());

        _mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<UserStats>>(),
                It.IsAny<FindOptions<UserStats, UserStats>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        // Act
        var result = await _service.GetNotificationHistoryCountAsync(userId);

        // Assert
        Assert.Equal(0, result);
    }

}