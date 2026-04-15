using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class ArmsKpiService : IArmsKpiService
    {
        private readonly IMongoCollection<KpiSubmission> _kpiCollection;
        private readonly IMongoCollection<KpiConfiguration> _configCollection;
        private readonly ILogger<ArmsKpiService> _logger;

        public ArmsKpiService(ILogger<ArmsKpiService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _kpiCollection = mongoDatabase.GetCollection<KpiSubmission>("KPI_Data");
            _configCollection = mongoDatabase.GetCollection<KpiConfiguration>("KPI_Configurations");
            _logger.LogInformation("ArmsKpiService initialized via Dependency Injection.");
        }

        public async Task<string> CreateOrUpdateSubmissionAsync(KpiSubmission submission)
        {
            // Define the "Identity" of this report
            var filter = Builders<KpiSubmission>.Filter.And(
                Builders<KpiSubmission>.Filter.Eq(x => x.MetaData.NetworkId, submission.MetaData.NetworkId),
                Builders<KpiSubmission>.Filter.Eq(x => x.MetaData.PeriodStart, submission.MetaData.PeriodStart)
            );

            var existing = await _kpiCollection.Find(filter).FirstOrDefaultAsync();

            if (existing == null)
            {
                submission.Id = null;
                submission.CreatedAt = DateTime.UtcNow;
                submission.UpdatedAt = null;

                await _kpiCollection.InsertOneAsync(submission);
            }
            else
            {
                submission.Id = existing.Id;
                submission.CreatedAt = existing.CreatedAt;
                submission.UpdatedAt = DateTime.UtcNow;

                await _kpiCollection.ReplaceOneAsync(filter, submission, new ReplaceOptions { IsUpsert = true });
            }


            _logger.LogInformation("Processed submission for {NetworkId}", submission.MetaData.NetworkId);

            return submission.Id!;
        }


        public async Task<KpiConfiguration?> GetConfigurationAsync(string networkId)
        {
            // 2. Find the configuration
            var config = await _configCollection
                .Find(x => x.NetworkId == networkId)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                _logger.LogWarning("No KPI configuration found for NetworkId: {NetworkId}", networkId);
                return null;
            }

            return config;
        }


        public async Task CreateOrUpdateConfigurationAsync(KpiConfiguration configuration)
        {
            var filter = Builders<KpiConfiguration>.Filter.Eq(x => x.NetworkId, configuration.NetworkId);

            var existing = await _configCollection.Find(filter).FirstOrDefaultAsync();

            if (existing == null)
            {
                configuration.CreatedAt = DateTime.UtcNow;
                configuration.UpdatedAt = null;

                configuration.Id = null;
            }
            else
            {
                configuration.Id = existing.Id;
                configuration.CreatedAt = existing.CreatedAt;
                configuration.UpdatedAt = DateTime.UtcNow;
            }

            //Upsert the document
            var options = new ReplaceOptions { IsUpsert = true };
            await _configCollection.ReplaceOneAsync(filter, configuration, options);

            _logger.LogInformation("KPI Configuration processed for NetworkId: {NetworkId}", configuration.NetworkId);
        }
    }
}
