using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class HeatRecoverySourceService : IHeatRecoverySourceService
    {
        private readonly IMongoCollection<HeatRecovery> _heatRecoverySourcesCollection;
        private readonly ILogger<HeatRecoverySourceService> _logger;

        public HeatRecoverySourceService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<HeatRecoverySourceService > logger)
        {
            _heatRecoverySourcesCollection = mongoDatabase.GetCollection<HeatRecovery>(dbSettings.Value.HeatRecoverySourcesCollectionName   );
            _logger = logger;
            _logger.LogInformation("HeatRecoverySourceService initialized via Dependency Injection.");
        }        
        public async Task<List<HeatRecovery>> GetAllAsync()
        {
            return await _heatRecoverySourcesCollection.Find(FilterDefinition<HeatRecovery>.Empty).ToListAsync();
        }        
    }
}
