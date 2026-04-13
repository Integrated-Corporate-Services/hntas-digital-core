using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Models.Arms;

namespace HNTAS.Core.Api.Validators.Arms
{
    public interface IKpiRuleValidator
    {
        Task<ValidationGateResult> ValidateAsync(KpiSubmissionRequest request);
    }
}
