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

        public SoaService(IOptions<AWSDocDbSettings> dbSettings, ILogger<SoaService> logger)
        {
            _logger = logger;

            var connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable.");
            }

            _logger.LogInformation("Initializing SoaService with connection string: {ConnectionString}", connectionString);

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            _heatNetworkCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
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



        public async Task UpdateElementLocationsAsync(string hnId, HeatNetworkElementType elementType, List<string> locations, string updatedBy)
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
            HeatNetworkElementType elementType,
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

        public async Task UpdateAssessmentPlanDocumentAsync(string hnId, AssessmentPlanDocument document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.Soa!.JourneyData.AssessmentPlans,
                    ap => ap.Phase == document.Phase)
            );

            var update = Builders<HeatNetwork>.Update
                .Set("soa.journeyData.assessmentPlans.$", document)
                .Set(hn => hn.Soa!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, document.UploadedBy);

            var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("soa.journeyData.assessmentPlans", document)
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
    }
}
