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

        public CountryAndTerritoryService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<CountryAndTerritoryService> logger)
        {
            _countriesAndTerritoriesCollection = mongoDatabase.GetCollection<CountryAndTerritory>(dbSettings.Value.CountriesAndTerritoriesCollectionName);
            _logger = logger;
            _logger.LogInformation("CountryAndTerritoryService initialized via Dependency Injection.");
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
