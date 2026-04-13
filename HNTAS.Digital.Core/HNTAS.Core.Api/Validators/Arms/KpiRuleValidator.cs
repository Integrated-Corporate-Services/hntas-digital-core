using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiRuleValidator : IKpiRuleValidator
    {
        private readonly IArmsKpiService _armsKpiService;
        private readonly ILogger<KpiRuleValidator> _logger;

        public KpiRuleValidator(IArmsKpiService armsKpiService, ILogger<KpiRuleValidator> logger)
        {
            _armsKpiService = armsKpiService;
            _logger = logger;
        }

        public async Task<ValidationGateResult> ValidateAsync(KpiSubmissionRequest request)
        {
            var config = await _armsKpiService.GetConfigurationAsync(request.MetaData.NetworkId);
            if (config == null)
            {
                _logger.LogWarning("KPI Submission failed: No configuration found for Network: {NetworkId}, PeriodStart: {PeriodStart}", request.MetaData.NetworkId, request.MetaData.PeriodStart);
                return new ValidationGateResult(false, "KPI Configuration not found for this network.");
            }

            return new ValidationGateResult(true);
        }
    }
}
