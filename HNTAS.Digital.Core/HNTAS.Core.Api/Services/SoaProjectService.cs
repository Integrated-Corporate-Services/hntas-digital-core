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

        public async Task<SoaProject> CreateAsync(string hnId)
        {
            var newProject = new SoaProject
            {
                HnId = hnId,
                Status = SoaProjectStatus.InProgress
            };

            await _soaProjectCollection.InsertOneAsync(newProject);
            return newProject;
        }

        public async Task<SoaProject> GetByHeatNetworkIdAsync(string hnId) =>
            await _soaProjectCollection.Find(p => p.HnId == hnId).FirstOrDefaultAsync();

        public async Task<SoaProject> GetByIdAsync(string id) =>
               await _soaProjectCollection.Find(p => p.Id == id).FirstOrDefaultAsync();


        public async Task UpdateConnectionTypesAsync(string hnId, List<ConnectionType> connectionTypes)
        {
            var filter = Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId);
            var update = Builders<SoaProject>.Update
                .Set(p => p.JourneyData.ConnectionTypes, connectionTypes)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }

        public async Task UpdateNetworkTypeAsync(string hnId, NetworkTypeSelection networkTypeSelection)
        {
            var filter = Builders<SoaProject>.Filter.Eq(p => p.HnId, hnId);

            // The C# driver automatically handles the enum conversion to a string for MongoDB
            var update = Builders<SoaProject>.Update
                .Set(p => p.JourneyData.NetworkType, networkTypeSelection)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _soaProjectCollection.UpdateOneAsync(filter, update);
        }


    }
}
