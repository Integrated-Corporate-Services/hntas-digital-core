using HNTAS.Core.Api.Data.Models.Arms.Submission;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IKpiSubmissionAuditService
    {
        Task TrackChangesAsync(KpiSubmission existing, KpiSubmission incoming);
    }
}
