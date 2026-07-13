using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class CountryAndTerritoryServiceTests
    {
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockOptions;
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<CountryAndTerritory>> _mockCollection;
        private readonly Mock<ILogger<CountryAndTerritoryService>> _mockLogger;

        public CountryAndTerritoryServiceTests()
        {
            _mockOptions = new Mock<IOptions<AWSDocDbSettings>>();
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<CountryAndTerritory>>();
            _mockLogger = new Mock<ILogger<CountryAndTerritoryService>>();

            _mockOptions.Setup(o => o.Value).Returns(new AWSDocDbSettings
            {
                CountriesAndTerritoriesCollectionName = "TestCountriesCollection"
            });

            // Set up the mock database to return our mock collection
            _mockMongoDatabase
                .Setup(db => db.GetCollection<CountryAndTerritory>(
                    "TestCountriesCollection",
                    It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockCollection.Object);
        }

        private CountryAndTerritoryService CreateService()
        {
            return new CountryAndTerritoryService(
                _mockOptions.Object,
                _mockMongoDatabase.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ExistsAsync_WhenCountryExists_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();
            string countryName = "United Kingdom";

            // Mock CountDocumentsAsync to return 1 (meaning it found the document)
            _mockCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<CountryAndTerritory>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await service.ExistsAsync(countryName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_WhenCountryDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            string countryName = "Unknown Country";

            // Mock CountDocumentsAsync to return 0
            _mockCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<CountryAndTerritory>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await service.ExistsAsync(countryName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsListOfCountries()
        {
            // Arrange
            var service = CreateService();
            var expectedList = new List<CountryAndTerritory>
            {
                new CountryAndTerritory { Id = "1", Name = "United Kingdom" },
                new CountryAndTerritory { Id = "2", Name = "France" }
            };

            // Mocking MongoDB's Find Fluent interface requires mocking IAsyncCursor
            var mockCursor = new Mock<IAsyncCursor<CountryAndTerritory>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<CountryAndTerritory>>(),
                    It.IsAny<FindOptions<CountryAndTerritory, CountryAndTerritory>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("United Kingdom", result[0].Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenIdExists_ReturnsCountry()
        {
            // Arrange
            var service = CreateService();
            var country = new CountryAndTerritory { Id = "123", Name = "Canada" };
            var expectedList = new List<CountryAndTerritory> { country };

            var mockCursor = new Mock<IAsyncCursor<CountryAndTerritory>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            _mockCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<CountryAndTerritory>>(),
                    It.IsAny<FindOptions<CountryAndTerritory, CountryAndTerritory>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetByIdAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123", result.Id);
            Assert.Equal("Canada", result.Name);
        }
    }
}