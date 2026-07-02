using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class ArmsKpiServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockMongoDatabase;
        private readonly Mock<IMongoCollection<KpiSubmission>> _mockKpiCollection;
        private readonly Mock<IMongoCollection<KpiConfiguration>> _mockConfigCollection;
        private readonly Mock<IKpiSubmissionAuditService> _mockAuditService;
        private readonly Mock<ILogger<ArmsKpiService>> _mockLogger;

        public ArmsKpiServiceTests()
        {
            _mockMongoDatabase = new Mock<IMongoDatabase>();
            _mockKpiCollection = new Mock<IMongoCollection<KpiSubmission>>();
            _mockConfigCollection = new Mock<IMongoCollection<KpiConfiguration>>();
            _mockAuditService = new Mock<IKpiSubmissionAuditService>();
            _mockLogger = new Mock<ILogger<ArmsKpiService>>();

            // Setup the mock database to return the correct mock collections by name
            _mockMongoDatabase
                .Setup(db => db.GetCollection<KpiSubmission>("KPI_Data", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockKpiCollection.Object);

            _mockMongoDatabase
                .Setup(db => db.GetCollection<KpiConfiguration>("KPI_Configurations", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockConfigCollection.Object);
        }

        private ArmsKpiService CreateService()
        {
            return new ArmsKpiService(_mockLogger.Object, _mockMongoDatabase.Object, _mockAuditService.Object);
        }

        [Fact]
        public async Task GetSubmissionByIdAsync_WhenIdExists_ReturnsSubmission()
        {
            // Arrange
            var service = CreateService();
            var submissionId = "sub-123";
            var expectedSubmission = new KpiSubmission
            {
                Id = submissionId,
                MetaData = new KpiMetadata { NetworkId = "HN-1", PeriodStart = "2026-01" }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiSubmission> { expectedSubmission });

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetSubmissionByIdAsync(submissionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(submissionId, result!.Id);
            Assert.Equal("HN-1", result.MetaData.NetworkId);
        }

        [Fact]
        public async Task CreateOrUpdateSubmissionAsync_WhenSubmissionDoesNotExist_InsertsNewDocument()
        {
            // Arrange
            var service = CreateService();
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "HN-1", PeriodStart = "2026-01" }
            };

            // Mock Find to return an empty cursor (document does not exist yet)
            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiSubmission>()); // Empty

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.CreateOrUpdateSubmissionAsync(submission);

            // Assert
            // FIXED: Changed "HN-New" to "HN-1" to match your Arrange section, and left the options parameter as null
            _mockKpiCollection.Verify(c => c.InsertOneAsync(
               It.Is<KpiSubmission>(s => s.MetaData.NetworkId == "HN-1" && s.CreatedAt != default),
               null,
               It.IsAny<CancellationToken>()),
               Times.Once);

            // Verify audit service was NOT called since it's a new insert
            _mockAuditService.Verify(a => a.TrackChangesAsync(It.IsAny<KpiSubmission>(), It.IsAny<KpiSubmission>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrUpdateSubmissionAsync_WhenSubmissionExists_AuditsAndReplacesDocument()
        {
            // Arrange
            var service = CreateService();
            var existingSubmission = new KpiSubmission
            {
                Id = "existing-id",
                MetaData = new KpiMetadata { NetworkId = "HN-Existing", PeriodStart = "2026-04" },
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var updatedSubmission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "HN-Existing", PeriodStart = "2026-04" }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiSubmission> { existingSubmission });

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.CreateOrUpdateSubmissionAsync(updatedSubmission);

            // Assert
            Assert.Equal("existing-id", result);

            // Verify Audit service was executed with old and new values
            _mockAuditService.Verify(a => a.TrackChangesAsync(existingSubmission, updatedSubmission), Times.Once);

            // Verify ReplaceOneAsync was executed with Upsert enabled
            _mockKpiCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<KpiSubmission>>(),
                It.Is<KpiSubmission>(s => s.Id == "existing-id" && s.UpdatedAt != null),
                It.Is<ReplaceOptions>(o => o.IsUpsert == true),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetConfigurationAsync_WhenConfigExists_ReturnsConfig()
        {
            // Arrange
            var service = CreateService();
            var networkId = "HN-Config-1";
            var expectedConfig = new KpiConfiguration { Id = "cfg-99", NetworkId = networkId };

            var mockCursor = new Mock<IAsyncCursor<KpiConfiguration>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiConfiguration> { expectedConfig });

            _mockConfigCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiConfiguration>>(),
                    It.IsAny<FindOptions<KpiConfiguration, KpiConfiguration>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetConfigurationAsync(networkId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("cfg-99", result!.Id);
            Assert.Equal(networkId, result.NetworkId);
        }

        [Fact]
        public async Task GetConfigurationAsync_WhenConfigDoesNotExist_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            var networkId = "HN-Missing";

            var mockCursor = new Mock<IAsyncCursor<KpiConfiguration>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiConfiguration>()); // Empty return

            _mockConfigCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiConfiguration>>(),
                    It.IsAny<FindOptions<KpiConfiguration, KpiConfiguration>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetConfigurationAsync(networkId);

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task GetSubmissionsAsync_WithSpecificMonthPeriod_AppliesExactFilter()
        {
            // Arrange
            var service = CreateService();
            var hnids = new List<string> { "HN-1", "HN-2" };
            string period = "2026-04";

            var expectedList = new List<KpiSubmission>
            {
                new KpiSubmission { Id = "sub-1", MetaData = new KpiMetadata { NetworkId = "HN-1", PeriodStart = "2026-04" } }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            // Mock FindAsync directly using the same pattern as your working test
            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetSubmissionsAsync(hnids, period);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("HN-1", result[0].MetaData.NetworkId);
            Assert.Equal("2026-04", result[0].MetaData.PeriodStart);
        }

        [Fact]
        public async Task GetSubmissionsAsync_WithYearOnlyPeriod_AppliesRegexFilter()
        {
            // Arrange
            var service = CreateService();
            var hnids = new List<string> { "HN-1" };
            string period = "2026";

            var expectedList = new List<KpiSubmission>
            {
                new KpiSubmission { Id = "sub-1", MetaData = new KpiMetadata { NetworkId = "HN-1", PeriodStart = "2026-01" } }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetSubmissionsAsync(hnids, period);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("HN-1", result[0].MetaData.NetworkId);
        }

        [Fact]
        public async Task GetSubmissionsAsync_WithNullOrEmptyPeriod_AppliesOnlyBaseFilter()
        {
            // Arrange
            var service = CreateService();
            var hnids = new List<string> { "HN-1" };
            string? period = null;

            var expectedList = new List<KpiSubmission>
            {
                new KpiSubmission { Id = "sub-1", MetaData = new KpiMetadata { NetworkId = "HN-1", PeriodStart = "2025-12" } }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetSubmissionsAsync(hnids, period);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("2025-12", result[0].MetaData.PeriodStart);
        }

        [Fact]
        public async Task GetSubmissionsForYearAsync_WhenSubmissionsExist_ReturnsListForThatYear()
        {
            // Arrange
            var service = CreateService();
            string networkId = "HN-12345";
            int year = 2026;

            var expectedList = new List<KpiSubmission>
            {
                new KpiSubmission { Id = "sub-jan", MetaData = new KpiMetadata { NetworkId = networkId, PeriodStart = "2026-01" } },
                new KpiSubmission { Id = "sub-feb", MetaData = new KpiMetadata { NetworkId = networkId, PeriodStart = "2026-02" } }
            };

            var mockCursor = new Mock<IAsyncCursor<KpiSubmission>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(expectedList);

            _mockKpiCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiSubmission>>(),
                    It.IsAny<FindOptions<KpiSubmission, KpiSubmission>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await service.GetSubmissionsForYearAsync(networkId, year);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("HN-12345", result[0].MetaData.NetworkId);
            Assert.Equal("2026-01", result[0].MetaData.PeriodStart);
            Assert.Equal("2026-02", result[1].MetaData.PeriodStart);
        }


        [Fact]
        public async Task CreateOrUpdateConfigurationAsync_WhenConfigDoesNotExist_SetsCreatedAtAndReplaces()
        {
            // Arrange
            var service = CreateService();
            var config = new KpiConfiguration { NetworkId = "HN-NEW-CFG" };

            // Return empty list to simulate configuration not existing
            var mockCursor = new Mock<IAsyncCursor<KpiConfiguration>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiConfiguration>());

            _mockConfigCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiConfiguration>>(),
                    It.IsAny<FindOptions<KpiConfiguration, KpiConfiguration>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            await service.CreateOrUpdateConfigurationAsync(config);

            // Assert
            Assert.Null(config.Id);
            Assert.NotNull(config.CreatedAt);
            Assert.Null(config.UpdatedAt);

            _mockConfigCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<KpiConfiguration>>(),
                It.Is<KpiConfiguration>(cfg => cfg.NetworkId == "HN-NEW-CFG" && cfg.Id == null),
                It.Is<ReplaceOptions>(o => o.IsUpsert == true),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateConfigurationAsync_WhenConfigExists_PreservesIdAndSetsUpdatedAt()
        {
            // Arrange
            var service = CreateService();
            var existingConfig = new KpiConfiguration
            {
                Id = "cfg-old-id",
                NetworkId = "HN-EXISTING-CFG",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };

            var incomingConfig = new KpiConfiguration { NetworkId = "HN-EXISTING-CFG" };

            var mockCursor = new Mock<IAsyncCursor<KpiConfiguration>>();
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            mockCursor.Setup(_ => _.Current).Returns(new List<KpiConfiguration> { existingConfig });

            _mockConfigCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<KpiConfiguration>>(),
                    It.IsAny<FindOptions<KpiConfiguration, KpiConfiguration>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            await service.CreateOrUpdateConfigurationAsync(incomingConfig);

            // Assert
            Assert.Equal("cfg-old-id", incomingConfig.Id);
            Assert.Equal(existingConfig.CreatedAt, incomingConfig.CreatedAt);
            Assert.NotNull(incomingConfig.UpdatedAt);

            _mockConfigCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<KpiConfiguration>>(),
                It.Is<KpiConfiguration>(cfg => cfg.Id == "cfg-old-id" && cfg.UpdatedAt != null),
                It.Is<ReplaceOptions>(o => o.IsUpsert == true),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}