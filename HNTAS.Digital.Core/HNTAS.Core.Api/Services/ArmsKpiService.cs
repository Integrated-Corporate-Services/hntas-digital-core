using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class ArmsKpiService : IArmsKpiService
    {
        private readonly IMongoCollection<KpiSubmission> _kpiCollection;
        private readonly ILogger<ArmsKpiService> _logger;

        public ArmsKpiService(ILogger<ArmsKpiService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _kpiCollection = mongoDatabase.GetCollection<KpiSubmission>("KPI_Data");
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

            if (existing != null)
            {
                submission.SubmissionId = existing.SubmissionId;

                await _kpiCollection.ReplaceOneAsync(filter, submission);
                _logger.LogInformation("Updated existing submission for {NetworkId}", submission.MetaData.NetworkId);
            }
            else
            {
                await _kpiCollection.InsertOneAsync(submission);
                _logger.LogInformation("Created new submission for {NetworkId}", submission.MetaData.NetworkId);
            }

            return submission.SubmissionId!;
        }
    }
}
