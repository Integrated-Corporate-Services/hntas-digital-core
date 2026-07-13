using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Models.Arms.V2;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ISubmissionCarbonCalculator
    {
        Task ProcessCarbonCalculationsAsync(KpiSubmissionRequestV2 request, KpiSubmission dataModel);
    }
}
