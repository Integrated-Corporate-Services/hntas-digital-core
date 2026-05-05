using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IArmsKpiService
    {
        Task<List<KpiSubmission>> GetSubmissionsAsync(List<string> hnids, string period);

        Task<KpiSubmission?> GetSubmissionByIdAsync(string submissionId);

        Task<string> CreateOrUpdateSubmissionAsync(KpiSubmission submission);

        Task<KpiConfiguration?> GetConfigurationAsync(string networkId);

        Task CreateOrUpdateConfigurationAsync(KpiConfiguration configuration);
    }
}
