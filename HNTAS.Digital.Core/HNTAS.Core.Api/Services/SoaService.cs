using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
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

        public async Task UpdateSoaDocumentAsync(string hnId, NetworkDetailsUploadedDocument document, string elementId, SoaStage stage)
        {
            try
            {
                // First, ensure ElementSoa is initialized
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.ElementSoa, null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set(
                    hn => hn.ElementSoa,
                    new ElementSoa
                    {
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = document.UploadedBy,
                        Status = NetworkDetailsStatus.InProgress,
                        Elements = new List<Elements>()
                    }
                );

                await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                // Try to update existing document for the specific stage and element
                var updateFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch<Elements>("elementSoa.elements",
                        new MongoDB.Bson.BsonDocument
                        {
                        { "elementId", elementId },
                        { "stages.stageId", stage.ToString() }
                        })
                );

                var update = Builders<HeatNetwork>.Update
                    .Set("elementSoa.elements.$[element].stages.$[stage].document", document)
                    .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
                    .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.InProgress);

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
                    // check if the stage exists for the existing element, if not, push the stage and document to the existing element
                    var stageExistsFilter = Builders<HeatNetwork>.Filter.And(
                        Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                        Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.ElementSoa!.Elements, e => e.ElementId == elementId && e.Stages.Any(s => s.StageId != stage))
                    );

                    var stageExistsUpdate = Builders<HeatNetwork>.Update
                        .Push("elementSoa.elements.$[element].stages", new SoaStages
                        {
                            StageId = stage,
                            Document = document
                        })
                        .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
                        .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
                        .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.InProgress);

                    var stageExistsArrayFilters = new[] 
                    {
                        new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                            new MongoDB.Bson.BsonDocument("element.elementId", elementId))
                    };

                    var stageExistsUpdateOptions = new UpdateOptions { ArrayFilters = stageExistsArrayFilters };
                    result = await _heatNetworkCollection.UpdateOneAsync(stageExistsFilter, stageExistsUpdate, stageExistsUpdateOptions);


                    if (result.ModifiedCount > 0)
                    {
                        _logger.LogInformation("Added document to existing element for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", hnId, stage, elementId);
                        return;
                    }

                    var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);


                    // Check if element exists, if not create it with the element and document
                    var insertUpdate = Builders<HeatNetwork>.Update
                        .Push("elementSoa.elements", new Elements
                        {
                            ElementId = elementId,
                            Stages = new List<SoaStages>
                            {
                            new SoaStages
                            {
                                StageId = stage,
                                Document = document
                            }
                            }
                        })
                        .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
                        .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
                        .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.InProgress);

                    await _heatNetworkCollection.UpdateOneAsync(insertFilter, insertUpdate);
                }

                _logger.LogInformation("Updated ElementSoa document for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", hnId, stage, elementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SOA document for HN ID: {HnId}, Element: {Element}, Stage: {Stage}", hnId, elementId, stage);
                throw;
            }
            
        }


        //public async Task UpdateSoaDocumentAsync(string hnId, NetworkDetailsUploadedDocument document, string elementId, SoaStage stage)
        //{
        //    try
        //    {
        //        // Step 1: Ensure ElementSoa is initialized
        //        var initFilter = Builders<HeatNetwork>.Filter.And(
        //            Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
        //            Builders<HeatNetwork>.Filter.Eq(hn => hn.ElementSoa, null)
        //        );

        //        var initUpdate = Builders<HeatNetwork>.Update.Set(
        //            hn => hn.ElementSoa,
        //            new ElementSoa
        //            {
        //                CreatedAt = DateTime.UtcNow,
        //                CreatedBy = document.UploadedBy,
        //                Status = NetworkDetailsStatus.Complete,
        //                Stages = new List<SoaStages>()
        //            }
        //        );

        //        await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

        //        // Step 2: Try to push document to existing stage and element
        //        var pushDocumentFilter = Builders<HeatNetwork>.Filter.And(
        //            Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
        //            Builders<HeatNetwork>.Filter.ElemMatch<SoaStages>("elementSoa.stages",
        //                new MongoDB.Bson.BsonDocument
        //                {
        //                    { "stage", stage },
        //                    { "elements", new MongoDB.Bson.BsonDocument
        //                        {
        //                            { "$elemMatch", new MongoDB.Bson.BsonDocument("elementId", elementId) }
        //                        }
        //                    }
        //                })
        //        );

        //        var pushDocumentUpdate = Builders<HeatNetwork>.Update
        //            .Push("elementSoa.stages.$[stage].elements.$[element].documents", document)
        //            .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
        //            .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
        //            .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.Complete);

        //        var arrayFilters = new[]
        //        {
        //            new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
        //                new MongoDB.Bson.BsonDocument("stage.stage", stage)),
        //            new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
        //                new MongoDB.Bson.BsonDocument("element.elementId", elementId))
        //        };

        //        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        //        var result = await _heatNetworkCollection.UpdateOneAsync(pushDocumentFilter, pushDocumentUpdate, updateOptions);

        //        if (result.ModifiedCount > 0)
        //        {
        //            _logger.LogInformation("Added document to existing ElementSoa stage and element for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", hnId, stage, elementId);
        //            return;
        //        }

        //        // Step 3: Check if stage exists but element doesn't - push element to existing stage
        //        var stageExistsFilter = Builders<HeatNetwork>.Filter.And(
        //            Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
        //            Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.ElementSoa!.Stages, s => s.Stage == stage)
        //        );

        //        var pushElementUpdate = Builders<HeatNetwork>.Update
        //            .Push("elementSoa.stages.$[stage].elements", new Elements
        //            {
        //                ElementId = elementId,
        //                Documents = new List<NetworkDetailsUploadedDocument> { document }
        //            })
        //            .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
        //            .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
        //            .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.Complete);

        //        var stageArrayFilters = new[]
        //        {
        //            new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
        //                new MongoDB.Bson.BsonDocument("stage.stage", stage))
        //        };

        //        var stageUpdateOptions = new UpdateOptions { ArrayFilters = stageArrayFilters };
        //        result = await _heatNetworkCollection.UpdateOneAsync(stageExistsFilter, pushElementUpdate, stageUpdateOptions);

        //        if (result.ModifiedCount > 0)
        //        {
        //            _logger.LogInformation("Added element to existing ElementSoa stage for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", hnId, stage, elementId);
        //            return;
        //        }

        //        // Step 4: Stage doesn't exist - create new stage with element and document
        //        var pushStageFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

        //        var pushStageUpdate = Builders<HeatNetwork>.Update
        //            .Push(hn => hn.ElementSoa!.Stages, new SoaStages
        //            {
        //                Stage = stage,
        //                Elements = new List<Elements>
        //                {
        //                    new Elements
        //                    {
        //                        ElementId = elementId,
        //                        Documents = new List<NetworkDetailsUploadedDocument> { document }
        //                    }
        //                }
        //            })
        //            .Set(hn => hn.ElementSoa!.UpdatedAt, DateTime.UtcNow)
        //            .Set(hn => hn.ElementSoa.UpdatedBy, document.UploadedBy)
        //            .Set(hn => hn.ElementSoa.Status, NetworkDetailsStatus.Complete);

        //        await _heatNetworkCollection.UpdateOneAsync(pushStageFilter, pushStageUpdate);

        //        _logger.LogInformation("Created new ElementSoa stage with element for HN ID: {HnId}, Stage: {Stage}, Element: {Element}", hnId, stage, elementId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error updating SOA document for HN ID: {HnId}, Element: {Element}, Stage: {Stage}", hnId, elementId, stage);
        //        throw;
        //    }
        //}
    }
}
