using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class ChpOperationalModeService : IChpOperationalModeService
    {
        private readonly IMongoCollection<CHPOperationalMode> _chpOperationalModesCollection;
        private readonly ILogger<ChpOperationalModeService> _logger;

        public ChpOperationalModeService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<ChpOperationalModeService> logger)
        {
            _chpOperationalModesCollection = mongoDatabase.GetCollection<CHPOperationalMode>(dbSettings.Value.ChpOperationalModesCollectionName);
            _logger = logger;
            _logger.LogInformation("ChpOperationalModeService initialized via Dependency Injection.");
        }        
        public async Task<List<CHPOperationalMode>> GetAllAsync()
        {
            return await _chpOperationalModesCollection.Find(FilterDefinition<CHPOperationalMode>.Empty).ToListAsync();
        }        
    }
}
