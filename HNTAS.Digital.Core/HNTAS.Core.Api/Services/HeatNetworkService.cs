using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class HeatNetworkService : IHeatNetworkService
    {
        private readonly IMongoCollection<HeatNetwork> _hnCollection;
        private readonly ILogger<HeatNetworkService> _logger;

        public HeatNetworkService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<HeatNetworkService> logger)
        {
            _hnCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
            _logger = logger;
            _logger.LogInformation("HeatNetworkService initialized via Dependency Injection.");
        }

        public async Task CreateAsync(HeatNetwork newHeatNetwork) =>
            await _hnCollection.InsertOneAsync(newHeatNetwork);

        public async Task<List<HeatNetwork>> GetAsync()
        {
            return await _hnCollection.Find(_ => true).ToListAsync();
        }

        public async Task<HeatNetwork> GetByHnIdAsync(string hnId)
        {
            return await _hnCollection.Find(hn => hn.HnId == hnId).FirstOrDefaultAsync();
        }

        public async Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> hnIds)
        {
            var filter = Builders<HeatNetwork>.Filter.In(hn => hn.HnId, hnIds);
            return await _hnCollection.Find(filter).ToListAsync();
        }
    }
}
