using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Models.Arms.Dashboard;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IKpiSubmissionAuditService
    {
        Task<IEnumerable<KpiHistoryResponse>> GetHistoryBySubmissionIdAsync(string submissionId);
        Task TrackChangesAsync(KpiSubmission existing, KpiSubmission incoming);
    }
}
