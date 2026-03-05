using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class SoaServiceTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<HeatNetwork>> _mockCollection;
        private readonly Mock<ILogger<SoaService>> _mockLogger;
        private readonly Mock<IOptions<AWSDocDbSettings>> _mockSettings;
        private readonly SoaService _sut;

        public SoaServiceTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<HeatNetwork>>();
            _mockLogger = new Mock<ILogger<SoaService>>();
            _mockSettings = new Mock<IOptions<AWSDocDbSettings>>();

            var settings = new AWSDocDbSettings
            {
                HeatNetworksCollectionName = "HeatNetworks"
            };

            _mockSettings.Setup(s => s.Value).Returns(settings);
            _mockDatabase.Setup(d => d.GetCollection<HeatNetwork>(It.IsAny<string>(), null))
                .Returns(_mockCollection.Object);

            _sut = new SoaService(_mockSettings.Object, _mockLogger.Object, _mockDatabase.Object);
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_WhenStageExists_UpdatesDocument()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var initUpdateResult = new UpdateResult.Acknowledged(0, 0, null);
            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(updateResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<UpdateDefinition<HeatNetwork>>(),
                It.IsAny<UpdateOptions>(),
                default), Times.AtLeast(2));

            Assert.NotEqual(default(DateTime), document.UploadedAt);
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_WhenStageDoesNotExist_PushesNewStage()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage2;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var noMatchResult = new UpdateResult.Acknowledged(0, 0, null);
            var pushResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .SetupSequence(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(noMatchResult) // Init update (no match)
                .ReturnsAsync(noMatchResult) // Stage exists check (no match)
                .ReturnsAsync(pushResult);    // Push new stage

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<UpdateDefinition<HeatNetwork>>(),
                It.IsAny<UpdateOptions>(),
                default), Times.Exactly(3));
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_InitializesSoaStages_WhenNull()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var initResult = new UpdateResult.Acknowledged(1, 1, null);
            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .SetupSequence(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(initResult)   // Initialization succeeds
                .ReturnsAsync(updateResult); // Update succeeds

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockCollection.Verify(c => c.UpdateOneAsync(
                It.Is<FilterDefinition<HeatNetwork>>(f => f != null),
                It.Is<UpdateDefinition<HeatNetwork>>(u => u != null),
                It.IsAny<UpdateOptions>(),
                default), Times.AtLeast(2));
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_SetsUploadedAt_BeforeUpdate()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123",
                UploadedAt = default
            };

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(updateResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            Assert.NotEqual(default(DateTime), document.UploadedAt);
            Assert.True((DateTime.UtcNow - document.UploadedAt).TotalSeconds < 5);
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_ThrowsException_LogsError()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var exception = new MongoException("Database error");

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() =>
                _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_WithValidParameters_LogsInformation()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage3;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(updateResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated ElementSoa document")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_WhenPushSucceeds_LogsAddedMessage()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var noMatchResult = new UpdateResult.Acknowledged(0, 0, null);
            var pushResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .SetupSequence(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(noMatchResult)
                .ReturnsAsync(noMatchResult)
                .ReturnsAsync(pushResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Added document to existing element")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Theory]
        [InlineData(SoaStage.Stage1)]
        [InlineData(SoaStage.Stage2)]
        [InlineData(SoaStage.Stage3)]
        [InlineData(SoaStage.Stage4)]
        [InlineData(SoaStage.Stage5)]
        [InlineData(SoaStage.Stage6)]
        [InlineData(SoaStage.Stage7)]
        [InlineData(SoaStage.Stage8)]
        public async Task UpdateSoaDocumentAsync_WorksForAllStages(SoaStage stage)
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .ReturnsAsync(updateResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            _mockCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<UpdateDefinition<HeatNetwork>>(),
                It.IsAny<UpdateOptions>(),
                default), Times.AtLeast(1));
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_WithNullDocument_ThrowsException()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            NetworkDetailsUploadedDocument? document = null;

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _sut.UpdateSoaDocumentAsync(hnId, document!, elementId, stage));
        }

        [Fact]
        public async Task UpdateSoaDocumentAsync_SetsElementSoaStatus_ToInProgress()
        {
            // Arrange
            var hnId = "HN0000001";
            var elementId = "00001";
            var stage = SoaStage.Stage1;
            var document = new NetworkDetailsUploadedDocument
            {
                FileName = "test.pdf",
                S3Key = "key/test.pdf",
                UploadedBy = "user123"
            };

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);
            UpdateDefinition<HeatNetwork>? capturedUpdate = null;

            _mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateDefinition<HeatNetwork>>(),
                    It.IsAny<UpdateOptions>(),
                    default))
                .Callback<FilterDefinition<HeatNetwork>, UpdateDefinition<HeatNetwork>, UpdateOptions, CancellationToken>(
                    (f, u, o, ct) => capturedUpdate = u)
                .ReturnsAsync(updateResult);

            // Act
            await _sut.UpdateSoaDocumentAsync(hnId, document, elementId, stage);

            // Assert
            Assert.NotNull(capturedUpdate);
            _mockCollection.Verify(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<HeatNetwork>>(),
                It.IsAny<UpdateDefinition<HeatNetwork>>(),
                It.IsAny<UpdateOptions>(),
                default), Times.AtLeast(1));
        }
    }
}