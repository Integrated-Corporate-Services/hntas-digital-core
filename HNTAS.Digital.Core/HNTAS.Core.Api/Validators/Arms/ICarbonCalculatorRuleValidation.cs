using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Data.Models.Arms.Submission;

namespace HNTAS.Core.Api.Validators.Arms
{
    public interface ICarbonCalculatorRuleValidation
    {
        public Task<ValidationGateResult> ValidateAsync(KpiSubmission dataModel);
    }
}
