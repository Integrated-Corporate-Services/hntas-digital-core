using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class AssessorServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<Assessor>> _mockCollection;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockOptions;
        private readonly Mock<ILogger<AssessorService>> _mockLogger;

        public AssessorServiceTests()
        {
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<Assessor>>();
            _mockOptions = new Mock<IOptions<AWSDocDbSettings>>();
            _mockLogger = new Mock<ILogger<AssessorService>>();

            _mockOptions.Setup(o => o.Value).Returns(new AWSDocDbSettings
            {
                AssessorsCollectionName = "TestAssessorsCollection"
            });

            _mockMongoDatabase
                .Setup(db => db.GetCollection<Assessor>("TestAssessorsCollection", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockCollection.Object);
        }

        private AssessorService CreateService()
        {
            return new AssessorService(_mockLogger.Object, _mockMongoDatabase.Object, _mockOptions.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetAssessorSuggestionsAsync_WhenSearchTermIsEmpty_ReturnsEmptyListImmediately(string? searchTerm)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAssessorSuggestionsAsync(searchTerm!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify the database was never hit
            _mockCollection.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Assessor>>(),
                It.IsAny<FindOptions<Assessor, Assessor>>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAssessorSuggestionsAsync_WhenMatchesExist_ReturnsMappedSearchResults()
        {
            // Arrange
            var service = CreateService();
            string searchTerm = "John";

            var dbAssessors = new List<Assessor>
            {
                new Assessor
                {
                    Id = "user-abc",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@test.com",
                    FullNameWithEmail = "John Doe (john.doe@test.com)",
                    Status = UserStatus.Active
                }
            };

            var mockCursor = new Mock<IAsyncCursor<Assessor>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(dbAssessors);

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Assessor>>(),
                    It.IsAny<FindOptions<Assessor, Assessor>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetAssessorSuggestionsAsync(searchTerm);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var item = result[0];
            Assert.Equal("user-abc", item.Id);
            Assert.Equal("John", item.FirstName);
            Assert.Equal("Doe", item.LastName);
            Assert.Equal("john.doe@test.com", item.Email);
            Assert.Equal("John Doe", item.FullName);
            Assert.Equal("John Doe (john.doe@test.com)", item.FullNameWithEmail);
        }

        [Fact]
        public async Task GetAssessorSuggestionsAsync_WhenDatabaseThrowsException_BubblesUpException()
        {
            // Arrange
            var service = CreateService();
            string searchTerm = "ErrorTrigger";

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Assessor>>(),
                    It.IsAny<FindOptions<Assessor, Assessor>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MongoException("Database query failed"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => service.GetAssessorSuggestionsAsync(searchTerm));
        }
    }
}