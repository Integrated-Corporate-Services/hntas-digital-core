using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class SoaService : ISoaService
    {
        private readonly IMongoCollection<HeatNetwork> _heatNetworkCollection;
        private readonly ILogger<SoaService> _logger;

        public SoaService(IOptions<AWSDocDbSettings> dbSettings, ILogger<SoaService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _heatNetworkCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
            _logger.LogInformation("SoaService initialized via Dependency Injection.");
        }

        public async Task<Soa> CreateAsync(string hnId, string createdBy)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);
            var update = Builders<HeatNetwork>.Update.Set(hn => hn.Soa, new Soa
            {
                Status = SoaStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
            });

            await _heatNetworkCollection.UpdateOneAsync(filter, update);
            var updated = await _heatNetworkCollection.Find(filter).FirstOrDefaultAsync();
            return updated?.Soa!;
        }

        public async Task<Soa?> UpdateStatusAsync(string hnId, SoaStatus newStatus, string updatedBy)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.Status, newStatus)
                .Set(hn => hn.Soa.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);

            var updated = await _heatNetworkCollection.Find(filter).FirstOrDefaultAsync();
            return updated?.Soa;
        }


        public async Task<Soa?> GetByHeatNetworkIdAsync(string hnId)
        {
            var heatNetwork = await _heatNetworkCollection
                .Find(hn => hn.HnId == hnId)
                .FirstOrDefaultAsync();

            return heatNetwork?.Soa;
        }




        public async Task UpdateConnectionTypesAsync(string hnId, string updatedBy, List<ConnectionType> connectionTypes)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.JourneyData.ConnectionTypes, connectionTypes)
                .Set(hn => hn.Soa.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);
        }


        public async Task UpdateNetworkTypeAsync(string hnId, string updatedBy, NetworkTypeSelection networkTypeSelection)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

            // Step 1: Initialize JourneyData only if it's null
            var initJourneyDataFilter = Builders<HeatNetwork>.Filter.And(
                filter,
                Builders<HeatNetwork>.Filter.Eq(hn => hn.Soa!.JourneyData, null)
            );

            var initJourneyDataUpdate = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.JourneyData, new SoaJourneyData());

            await _heatNetworkCollection.UpdateOneAsync(initJourneyDataFilter, initJourneyDataUpdate);

            // Step 2: Apply the actual update
            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.JourneyData.NetworkType, networkTypeSelection)
                .Set(hn => hn.Soa.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);

            _logger.LogInformation("Updated network type to {NetworkType} for HN ID: {HnId} by {UpdatedBy}", networkTypeSelection.Type, hnId, updatedBy);
        }



        public async Task UpdateHeatNetworkElementsAsync(string hnId, List<HeatNetworkElement> elements, string updatedBy)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.JourneyData.HeatNetworkElements, elements)
                .Set(hn => hn.Soa.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy);
            // .Set(hn => hn.Soa.Status, SoaProjectStatus.ElementsChosen); // Uncomment if status progression is needed

            await _heatNetworkCollection.UpdateOneAsync(filter, update);
        }



        public async Task UpdateElementLocationsAsync(string hnId, HeatNetworkElementDisplayType elementType, List<string> locations, string updatedBy)
        {
            var enumAsString = elementType.ToString();

            // Filter to find the HeatNetwork document with the matching hnId and the specific element in the embedded array
            var filter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.Eq("soa.journeyData.heatNetworkElements.name", enumAsString)
            );

            // Update the matched element's locations using the positional operator '$'
            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy)
                .Set("soa.journeyData.heatNetworkElements.$.locations", locations);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);
        }



        public async Task UpdateElementDocumentsAsync(
            string hnId,
            HeatNetworkElementDisplayType elementType,
            List<UploadedDocument> documents,
            string updatedBy)
        {
            var enumAsString = elementType.ToString();

            var filter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.Eq("soa.journeyData.heatNetworkElements.name", enumAsString)
            );

            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy)
                .Set("soa.journeyData.heatNetworkElements.$.documents", documents);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateAssessmentDocumentAsync(string hnId, Document document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.Soa!.JourneyData.AssessmentDocs,
                    ap => ap.Phase == document.Phase)
            );

            var update = Builders<HeatNetwork>.Update
                .Set("soa.journeyData.assessmentDocs.$", document)
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

            var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("soa.journeyData.assessmentDocs", document)
                    .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

                await _heatNetworkCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        }

        public async Task UpdateAssessorDocumentAsync(string hnId, Document document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.Soa!.JourneyData.AssessorDocs,
                    ad => ad.Phase == document.Phase)
            );

            var update = Builders<HeatNetwork>.Update
                .Set("soa.journeyData.assessorDocs.$", document)
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

            var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("soa.journeyData.assessorDocs", document)
                    .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

                await _heatNetworkCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        }

        public async Task UpdateCertifierDocumentAsync(string hnId, Document document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.Soa!.JourneyData.CertifierDocs,
                    cd => cd.Phase == document.Phase)
            );

            var update = Builders<HeatNetwork>.Update
                .Set("soa.journeyData.certifierDocs.$", document)
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

            var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("soa.journeyData.certifierDocs", document)
                    .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

                await _heatNetworkCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        } 

        public async Task DeleteByHeatNetworkIdAsync(string hnId)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);
            var update = Builders<HeatNetwork>.Update.Unset(hn => hn.Soa);

            var result = await _heatNetworkCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
            {
                throw new InvalidOperationException($"No embedded SOA data found to delete for HN ID: {hnId}");
            }
        }        

        public async Task UpdateSoaStatus(string hnId, string elementId, SoaStage stage, string soaStatus, string updatedBy, NetworkDetailsStatus elementSoaStatus)
        {
            try
            {
                // First, ensure SoaStages is initialized
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.NetworkElements!.Elements,
                        e => e.ElementId == elementId && e.SoaStages == null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set("networkElements.elements.$.soaStages", new List<SoaStages>());

                await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                // Try to update existing status for the specific stage and element
                var updateFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch<Element>("networkElements.elements",
                        new MongoDB.Bson.BsonDocument
                        {
                            { "elementId", elementId },
                            { "soaStages.stageId", stage.ToString() }
                        })
                );

                var update = Builders<HeatNetwork>.Update
                    .Set("networkElements.elements.$[element].soaStages.$[stage].soaStatus", soaStatus)
                    .Set("networkElements.elements.$[element].soaStages.$[stage].soaStatusUpdatedAt", DateTime.UtcNow)
                    .Set("networkElements.elements.$[element].soaStages.$[stage].soaStatusUpdatedBy", updatedBy)
                    .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("element.elementId", elementId)),
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("stage.stageId", stage.ToString()))
                };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
                var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update, updateOptions);

                if (result.ModifiedCount == 0)
                {
                    // Stage doesn't exist - add it using array filter
                    var pushFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                    var pushUpdate = Builders<HeatNetwork>.Update
                        .Push("networkElements.elements.$[element].soaStages", new SoaStages
                        {
                            StageId = stage,
                            SoaStatus = soaStatus,
                            SoaStatusUpdatedAt = DateTime.UtcNow,
                            SoaStatusUpdatedBy = updatedBy
                            //Document = document
                        })
                        .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                    var pushArrayFilters = new[]
                    {
                        new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                            new MongoDB.Bson.BsonDocument("element.elementId", elementId))
                    };

                    var pushOptions = new UpdateOptions { ArrayFilters = pushArrayFilters };
                    result = await _heatNetworkCollection.UpdateOneAsync(pushFilter, pushUpdate, pushOptions);

                    if (result.ModifiedCount > 0)
                    {
                        _logger.LogInformation("Added document to existing element for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", StringFormatter.Sanitize(hnId), stage, StringFormatter.Sanitize(elementId));
                        return;
                    }
                }

                _logger.LogInformation("Updated ElementSoa document for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", StringFormatter.Sanitize(hnId), stage, StringFormatter.Sanitize(elementId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SOA document for HN ID: {HnId}, Element: {Element}, Stage: {Stage}", StringFormatter.Sanitize(hnId), StringFormatter.Sanitize(elementId), stage);
                throw;
            }

        }
    }
}
