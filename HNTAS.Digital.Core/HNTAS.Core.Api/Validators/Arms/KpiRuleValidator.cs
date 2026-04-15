using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;

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

        public async Task<ValidationGateResult> ValidateAsync(KpiSubmission request)
        {
            var config = await _armsKpiService.GetConfigurationAsync(request.MetaData.NetworkId);
            if (config == null)
            {
                _logger.LogWarning("KPI Submission failed: No configuration found for Network: {NetworkId}, PeriodStart: {PeriodStart}", request.MetaData.NetworkId, request.MetaData.PeriodStart);
                return new ValidationGateResult(false, "KPI Configuration not found for this network.");
            }

            // Validate Aggregated KPIs
            foreach (var kpi in request.ConsumerConnectionAggregatedKpis)
            {
                // Find rule in config (assuming aggregated rules are stored under a specific element type)
                var rule = config.Elements
                    .FirstOrDefault(e => e.Type == ElementType.ConsumerConnection)?
                    .Kpis.GetValueOrDefault(kpi.Key);

                if (rule != null)
                {
                    kpi.Value.AssessmentStatus = Assess(kpi.Value.Value, rule);
                }
            }


            // Validate Individual Elements
            foreach (var element in request.Elements)
            {
                var elementConfig = config.Elements.FirstOrDefault(e => e.Type == element.Type);

                foreach (var kpi in element.Kpis)
                {
                    var rule = elementConfig?.Kpis.GetValueOrDefault(kpi.Key);

                    // If the rule exists, assess it; otherwise, mark as Undefined
                    kpi.Value.AssessmentStatus = rule != null ? Assess(kpi.Value.Value, rule) : KPIAssessmentStatus.Undefined;

                    if (rule == null)
                    {
                        _logger.LogDebug("KPI {KpiId} set to Undefined: No configuration found for element {ElementId}", kpi.Key, element.ElementId);
                    }
                }
            }

            return new ValidationGateResult(true);
        }


        // Logic to determine the Status flag
        private KPIAssessmentStatus Assess(double value, KpiRule rule)
        {
            // 1. Outside Limit Check (Physical/Logical Bounds)
            if (value < rule.LowerLimit || value > rule.UpperLimit)
            {
                return KPIAssessmentStatus.OutsideLimit;
            }

            var threshold = rule.ThresholdRule;
            bool metTarget = false;

            // 2. Performance Threshold Check
            switch (threshold.Type.ToLower())
            {
                case "gte": // Greater than or equal to
                    metTarget = value >= (threshold.Value ?? threshold.Target ?? 0);
                    break;

                case "lte": // Less than or equal to
                    metTarget = value <= (threshold.Value ?? threshold.Target ?? 0);
                    break;

                case "plus_minus": // Within a specific range of a target
                    if (threshold.Target.HasValue && threshold.Delta.HasValue)
                    {
                        double lowerTarget = threshold.Target.Value - threshold.Delta.Value;
                        double upperTarget = threshold.Target.Value + threshold.Delta.Value;
                        metTarget = value >= lowerTarget && value <= upperTarget;
                    }
                    break;

                case "equal":
                    metTarget = Math.Abs(value - (threshold.Value ?? 0)) < 0.000001;
                    break;

                default:
                    _logger.LogWarning("Unknown threshold type: {Type}", threshold.Type);
                    metTarget = false;
                    break;
            }

            return metTarget ? KPIAssessmentStatus.Pass : KPIAssessmentStatus.Fail;
        }
    }
}
