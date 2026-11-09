using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class FuelTypeService : IFuelTypeService
    {
        private readonly IMongoCollection<FuelType> _fuelTypesCollection;
        private readonly ILogger<FuelTypeService> _logger;

        public FuelTypeService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<FuelTypeService> logger)
        {
            _fuelTypesCollection = mongoDatabase.GetCollection<FuelType>(dbSettings.Value.FuelTypesCollectionName);
            _logger = logger;
            _logger.LogInformation("FuelTypeService initialized via Dependency Injection.");
        }        
        public async Task<List<FuelType>> GetAllAsync()
        {
            return await _fuelTypesCollection.Find(FilterDefinition<FuelType>.Empty).ToListAsync();
        }        
    }
}
