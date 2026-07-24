using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Models.Arms.PowerBi;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;



namespace HNTAS.Digital.Core.Tests.Services
{

    public class ArmsPowerBiServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<KpiSubmission>> _mockKpiCollection;
        private readonly Mock<IMongoCollection<User>> _mockUserCollection;
        private readonly Mock<ILogger<ArmsPowerBiService>> _mockLogger;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockDbSettings;
        private readonly ArmsPowerBiService _service;

        public ArmsPowerBiServiceTests()
        {
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockKpiCollection = new Mock<IMongoCollection<KpiSubmission>>();
            _mockLogger = new Mock<ILogger<ArmsPowerBiService>>();
            _mockDbSettings = new Mock<IOptions<AWSDocDbSettings>>();
            _mockUserCollection = new Mock<IMongoCollection<User>>();

            var settings = new AWSDocDbSettings
            {
                KPI_DataCollectionName = "KPI_Data",
                HeatNetworksCollectionName = "HeatNetworks",
                OrganisationsCollectionName = "Organisations",
                UsersCollectionName = "Users"
            };

            _mockDbSettings.Setup(s => s.Value).Returns(settings);

            // Setup the mock database to return your mock KPI collection
            _mockMongoDatabase
                .Setup(db => db.GetCollection<KpiSubmission>(settings.KPI_DataCollectionName, It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockKpiCollection.Object);

            _mockMongoDatabase
              .Setup(db => db.GetCollection<User>(settings.UsersCollectionName, It.IsAny<MongoCollectionSettings>()))
              .Returns(_mockUserCollection.Object);

            _service = new ArmsPowerBiService(_mockLogger.Object, _mockMongoDatabase.Object, _mockDbSettings.Object);
        }

        [Fact]
        public async Task GetPowerBiDataAsync_ReturnsExpectedData_WhenPipelineSucceeds()
        {
            // Arrange
            var expectedResults = new List<ArmsPowerBiReportResult>
            {
                new ArmsPowerBiReportResult
                {
                    OrgId = "org-777",
                    KpiSubmission = new KpiSubmission {
                        Id = "kpi-id-123" ,
                        MetaData = new KpiMetadata {
                            NetworkId = "HN2000001",
                            PeriodStart = "2026-01"
                        }
                    }
                }
            };

            // 1. Mock the IAsyncCursor that ToListAsync() loops through
            var mockCursor = new Mock<IAsyncCursor<ArmsPowerBiReportResult>>();

            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true)  // First check: item exists
                      .ReturnsAsync(false); // Second check: iteration ends

            mockCursor.Setup(c => c.Current).Returns(expectedResults);

            // 2. Intercept the AggregateAsync call on the collection
            _mockKpiCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<KpiSubmission, ArmsPowerBiReportResult>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _service.GetPowerBiDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("org-777", result[0].OrgId);
            Assert.Equal("kpi-id-123", result[0].KpiSubmission.Id);
        }

        [Fact]
        public async Task GetPowerBiDataAsync_LogsErrorAndThrows_WhenExceptionOccurs()
        {
            // Arrange
            _mockKpiCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<KpiSubmission, ArmsPowerBiReportResult>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MongoException("Connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() => _service.GetPowerBiDataAsync());

            // Verify the error was logged via your logger mock
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error executing Power BI aggregation pipeline.")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetPowerBiUserDataAsync_ShouldReturnActiveResponsiblePeople_WhenUsersExist()
        {
            // --- Arrange ---
            var expectedResults = new List<ArmsPowerBiUserReportResult>
            {
                new ArmsPowerBiUserReportResult
                {
                    UserId = "60c72b2f9b1d8b2bad000001",
                    HnId = "HN-001",
                    OrgId = "ORG-100"
                }
            };

            var mockCursor = new Mock<IAsyncCursor<ArmsPowerBiUserReportResult>>();

            // Set up the cursor to iterate and return our final data array
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedResults);

            // FIX: Mock the actual interface method 'AggregateAsync' instead of the extension method
            _mockUserCollection
                .Setup(c => c.AggregateAsync(
                    It.IsAny<PipelineDefinition<User, ArmsPowerBiUserReportResult>>(),
                    It.IsAny<AggregateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // --- Act ---
            var result = await _service.GetPowerBiUserDataAsync();

            // --- Assert ---
            Assert.NotNull(result);
            var singleResult = Assert.Single(result);
            Assert.Equal("60c72b2f9b1d8b2bad000001", singleResult.UserId);
            Assert.Equal("HN-001", singleResult.HnId);
            Assert.Equal("ORG-100", singleResult.OrgId);
        }

    }
}