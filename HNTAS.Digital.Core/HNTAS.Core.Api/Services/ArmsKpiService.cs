using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class ArmsKpiService : IArmsKpiService
    {
        private readonly IMongoCollection<KpiSubmission> _kpiCollection;
        private readonly IMongoCollection<KpiConfiguration> _configCollection;
        private readonly IKpiSubmissionAuditService _auditService;
        private readonly ILogger<ArmsKpiService> _logger;

        public ArmsKpiService(ILogger<ArmsKpiService> logger, IMongoDatabase mongoDatabase, IKpiSubmissionAuditService auditService, IOptions<AWSDocDbSettings> dbSettings)
        {
            _logger = logger;
            _kpiCollection = mongoDatabase.GetCollection<KpiSubmission>(dbSettings.Value.KPI_DataCollectionName);
            _configCollection = mongoDatabase.GetCollection<KpiConfiguration>(dbSettings.Value.KPI_ConfigurationsCollectionName);
            _logger.LogInformation("ArmsKpiService initialized via Dependency Injection.");
            _auditService = auditService;
        }

        public async Task<List<KpiSubmission>> GetSubmissionsAsync(List<string> hnids, string? period)
        {
            var filterBuilder = Builders<KpiSubmission>.Filter;

            // 1. Base filter for the allowed networks
            var filter = filterBuilder.In(x => x.MetaData.NetworkId, hnids);

            // 2. Adjust period filter based on whether a specific month is provided
            if (!string.IsNullOrEmpty(period) && period.Length > 5) // e.g. "2026-04"
            {
                filter &= filterBuilder.Eq(x => x.MetaData.PeriodStart, period);
            }
            else if (!string.IsNullOrEmpty(period)) // e.g. "2026"
            {
                // Matches any string starting with the year, like "2026-01", "2026-02", etc.
                filter &= filterBuilder.Regex(x => x.MetaData.PeriodStart, $"^{period}-");
            }

            // 3. Project only metadata to keep the query fast
            return await _kpiCollection.Find(filter)
                .Project<KpiSubmission>(Builders<KpiSubmission>.Projection
                    .Include(x => x.Id)
                    .Include(x => x.MetaData)
                    .Include(x => x.CreatedAt)
                    .Include(x => x.UpdatedAt))
                .ToListAsync();
        }

        public async Task<KpiSubmission?> GetSubmissionByIdAsync(string submissionId)
        {
            return await _kpiCollection
                .Find(s => s.Id == submissionId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<KpiSubmission>> GetSubmissionsForYearAsync(string networkId, int year)
        {
            var filterBuilder = Builders<KpiSubmission>.Filter;

            // 1. Filter by the specific heat network ID
            var filter = filterBuilder.Eq(x => x.MetaData.NetworkId, networkId);

            // 2. Filter by the calendar year using a Regex match (e.g., "^2026-")
            // This matches any period starting with the year format like "2026-01", "2026-02", etc.
            filter &= filterBuilder.Regex(x => x.MetaData.PeriodStart, $"^{year}-");

            // 3. Return full documents so CarbonInputsV2 payload data is populated for calculations
            return await _kpiCollection
                .Find(filter)
                .ToListAsync();
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

                // Audit the differences
                // We pass the 'existing' (old) and 'submission' (new)
                await _auditService.TrackChangesAsync(existing, submission);

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
