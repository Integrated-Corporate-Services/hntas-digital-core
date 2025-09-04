using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class SoaProjectService : ISoaProjectService
    {
        private readonly IMongoCollection<SoaProject> _soaProjectCollection;
        private readonly ILogger<SoaProjectService> _logger;

        public SoaProjectService(IOptions<AWSDocDbSettings> dbSettings, ILogger<SoaProjectService> logger)
        {
            _logger = logger;

            var connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable.");
            }

            _logger.LogInformation("Initializing UserService with connection string: {ConnectionString}", connectionString);

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            _soaProjectCollection = mongoDatabase.GetCollection<SoaProject>(dbSettings.Value.SoaProjectCollectionName);
        }

        public async Task<SoaProject> CreateAsync(string hnId, string createdBy)
        {
            var newProject = new SoaProject
            {
                HnId = hnId,
                Status = SoaProjectStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
            };

            await _soaProjectCollection.InsertOneAsync(newProject);
            return newProject;
        }

        public async Task<SoaProject> GetByHeatNetworkIdAsync(string hnId) =>
            await _soaProjectCollection.Find(p => p.HnId == hnId).FirstOrDefaultAsync();

        public async Task<SoaProject> GetByIdAsync(string id) =>
               await _soaProjectCollection.Find(p => p.Id == id).FirstOrDefaultAsync();


        public async Task UpdateConnectionTypesAsync(string hnId, string updatedBy, List<ConnectionType> connectionTypes)
        {
            var filter = Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId);
            var update = Builders<SoaProject>.Update
                .Set(p => p.JourneyData.ConnectionTypes, connectionTypes)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Set(p => p.UpdatedBy, updatedBy);

            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateNetworkTypeAsync(string hnId, string updatedBy, NetworkTypeSelection networkTypeSelection)
        {
            var filter = Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId);

            // Step 1: Ensure JourneyData is initialized if null
            var initJourneyDataFilter = Builders<SoaProject>.Filter.And(
                filter,
                Builders<SoaProject>.Filter.Eq(p => p.JourneyData, null)
            );

            var initJourneyDataUpdate = Builders<SoaProject>.Update
                .Set(p => p.JourneyData, new SoaJourneyData());

            await _soaProjectCollection.UpdateOneAsync(initJourneyDataFilter, initJourneyDataUpdate);

            // Step 2: Apply the actual update
            var update = Builders<SoaProject>.Update
                .Set(p => p.JourneyData.NetworkType, networkTypeSelection)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Set(p => p.UpdatedBy, updatedBy);

            await _soaProjectCollection.UpdateOneAsync(filter, update);

            _logger.LogInformation("Updated network type to {NetworkType} for HN ID: {HnId} by {UpdatedBy}", networkTypeSelection, hnId, updatedBy);
        }

        public async Task UpdateHeatNetworkElementsAsync(string hnId, List<HeatNetworkElement> elements, string updatedBy)
        {
            var filter = Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId);

            var update = Builders<SoaProject>.Update
                .Set(p => p.JourneyData.HeatNetworkElements, elements)
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Set(p => p.UpdatedBy, updatedBy);
            //.Set(p => p.Status, SoaProjectStatus.ElementsChosen); // Set the status to reflect the completed stage

            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }


        public async Task UpdateElementLocationsAsync(string hnId, HeatNetworkElementType elementType, List<string> locations, string updatedBy)
        {
            // Convert the enum to a string to match the database value
            var enumAsString = elementType.ToString();

            // Create a combined filter to find the correct document and the array element to update.
            // The filter must match the main document AND a specific element within the array.
            var filter = Builders<SoaProject>.Filter.And(
                Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId),
                Builders<SoaProject>.Filter.Eq("journeyData.heatNetworkElements.name", enumAsString)
            );

            // Define the update operations.
            // Use the positional operator '$' to update the 'locations' array of the matched element.
            var update = Builders<SoaProject>.Update
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Set(p => p.UpdatedBy, updatedBy)
                .Set("journeyData.heatNetworkElements.$.locations", locations);

            // Perform the update. The UpdateOneAsync method will use the filter to find the document
            // and then apply the update to the first array element that matched the filter criteria.
            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }


        public async Task UpdateElementDocumentsAsync(
            string hnId,
            HeatNetworkElementType elementType,
            List<UploadedDocument> documents,
            string updatedBy)
        {
            var enumAsString = elementType.ToString();

            var filter = Builders<SoaProject>.Filter.And(
                Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId),
                Builders<SoaProject>.Filter.Eq("journeyData.heatNetworkElements.name", enumAsString)
            );

            var update = Builders<SoaProject>.Update
                .Set(p => p.UpdatedAt, DateTime.UtcNow)
                .Set(p => p.UpdatedBy, updatedBy)
                .Set("journeyData.heatNetworkElements.$.documents", documents);

            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }

    }
}
