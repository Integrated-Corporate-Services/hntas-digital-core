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

        public async Task<List<HeatNetwork>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            // GOV.UK/ISO 8601 standard: Ensure we cover the full 'to' day
            // This sets toDate to 23:59:59 of that day
            var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

            var filter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Gte(x => x.CreatedAt, fromDate.Date),
                Builders<HeatNetwork>.Filter.Lte(x => x.CreatedAt, endOfDay)
            );

            return await _hnCollection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt) // Standard practice: show newest first
                .ToListAsync();
        }
    }
}
