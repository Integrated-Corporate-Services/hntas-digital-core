using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class CountryAndTerritoryService : ICountryAndTerritoryService
    {
        private readonly IMongoCollection<CountryAndTerritory> _countriesAndTerritoriesCollection;
        private readonly ILogger<CountryAndTerritoryService> _logger;

        public CountryAndTerritoryService(IOptions<AWSDocDbSettings> dbSettings, ILogger<CountryAndTerritoryService> logger)
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

            _countriesAndTerritoriesCollection = mongoDatabase.GetCollection<CountryAndTerritory>(dbSettings.Value.CountriesAndTerritoriesCollectionName);
        }

        public async Task<bool> ExistsAsync(string name)
        {
            var count = await _countriesAndTerritoriesCollection.CountDocumentsAsync(x => x.Name == name);
            return count > 0;
        }

        public async Task<List<CountryAndTerritory>> GetAllAsync()
        {
            return await _countriesAndTerritoriesCollection.Find(FilterDefinition<CountryAndTerritory>.Empty).ToListAsync();
        }

        public async Task<CountryAndTerritory?> GetByIdAsync(string id)
        {
            return await _countriesAndTerritoriesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
    }
}
