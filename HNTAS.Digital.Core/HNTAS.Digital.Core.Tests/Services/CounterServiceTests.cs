using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class CounterServiceTests
    {
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockOptions;
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<Counter>> _mockCollection;
        private readonly Mock<ILogger<CounterService>> _mockLogger;

        public CounterServiceTests()
        {
            _mockOptions = new Mock<IOptions<AWSDocDbSettings>>();
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<Counter>>();
            _mockLogger = new Mock<ILogger<CounterService>>();

            _mockOptions.Setup(o => o.Value).Returns(new AWSDocDbSettings
            {
                CountersCollectionName = "TestCountersCollection"
            });

            _mockMongoDatabase
                .Setup(db => db.GetCollection<Counter>("TestCountersCollection", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockCollection.Object);
        }

        private CounterService CreateService()
        {
            return new CounterService(_mockOptions.Object, _mockMongoDatabase.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetNextSequenceValue_WhenCounterIsPreInitialized_IncrementsAndReturnsValue()
        {
            // Arrange
            var service = CreateService();
            string sequenceName = "userId_sequence";
            var expectedCounter = new Counter { Id = sequenceName, SequenceValue = 2000005 };

            // Mock the FindOneAndUpdateAsync call to return our incremented counter directly
            _mockCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<Counter>>(),
                    It.IsAny<UpdateDefinition<Counter>>(),
                    It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCounter);

            // Act
            var result = await service.GetNextSequenceValue(sequenceName);

            // Assert
            Assert.Equal(2000005, result);
        }

        [Fact]
        public async Task GetNextSequenceValue_WhenCounterNotPreInitialized_ResetsToStartingValue()
        {
            // Arrange
            var service = CreateService();
            string sequenceName = "new_sequence";

            // First call returns 1 (it was just created/upserted by the Inc operation)
            var initialUpsertResult = new Counter { Id = sequenceName, SequenceValue = 1 };
            // Second call returns 2000001 (after the correction Set operation runs)
            var correctionResult = new Counter { Id = sequenceName, SequenceValue = 2000001 };

            _mockCollection
                .SetupSequence(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<Counter>>(),
                    It.IsAny<UpdateDefinition<Counter>>(),
                    It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialUpsertResult)   // First hit
                .ReturnsAsync(correctionResult);     // Second hit (inside the if block)

            // Act
            var result = await service.GetNextSequenceValue(sequenceName);

            // Assert
            Assert.Equal(2000001, result);
        }

        [Fact]
        public async Task GetNextSequenceValue_WhenDatabaseThrowsException_BubblesUpException()
        {
            // Arrange
            var service = CreateService();
            string sequenceName = "error_sequence";

            _mockCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<Counter>>(),
                    It.IsAny<UpdateDefinition<Counter>>(),
                    It.IsAny<FindOneAndUpdateOptions<Counter, Counter>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MongoException("Database connection timeout."));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => service.GetNextSequenceValue(sequenceName));
        }
    }
}