using HNTAS.Core.Api.Data.Models.Arms.Submission;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IArmsKpiService
    {
        Task<string> CreateOrUpdateSubmissionAsync(KpiSubmission submission);
    }
}
