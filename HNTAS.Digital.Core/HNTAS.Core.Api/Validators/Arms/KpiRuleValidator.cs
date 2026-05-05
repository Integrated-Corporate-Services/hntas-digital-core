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

        // This handles floating-point precision errors (e.g., 75.0000000001 vs 75)
        private const double GlobalEpsilon = 1e-6;


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
                _logger.LogWarning("KPI Submission failed: No config for Network: {NetworkId}", request.MetaData.NetworkId);
                return new ValidationGateResult(false, "KPI Configuration not found for this network.");
            }

            var configLookup = config.Elements.ToDictionary(e => e.Type, e => e.Kpis);
            var errors = new List<string>();

            // 1. Validate Aggregated KPIs
            if (request.ConsumerConnectionAggregatedKpis != null && configLookup.TryGetValue(HeatNetworkElementType.ConsumerConnection, out var aggRules))
            {
                foreach (var (kpiId, kpiValue) in request.ConsumerConnectionAggregatedKpis)
                {
                    if (aggRules.TryGetValue(kpiId, out var rule))
                    {
                        kpiValue.AssessmentStatus = Assess(kpiValue.Value, rule);
                    }
                }
            }

            // Define the aggregated IDs to exclude from element-level checks
            var aggregatedKpis = new[] { "CC-KPI-01", "CC-KPI-02", "CC-KPI-03" };

            // 2. Validate Individual Elements
            foreach (var element in request.Elements)
            {
                if (!configLookup.TryGetValue(element.Type, out var elementKpiRules))
                {
                    _logger.LogDebug("No config rules found for element type: {ElementType}", element.Type);
                    continue;
                }

                var missingMandatory = elementKpiRules
                .Where(r => r.Value.IsMandatory && !aggregatedKpis.Contains(r.Key) && !element.Kpis.ContainsKey(r.Key))
                .Select(r => r.Key);

                foreach (var missingKey in missingMandatory)
                {
                    errors.Add($"Element ID '{element.ElementId}' validation error: Missing mandatory KPI '{missingKey}'.");
                }

                foreach (var (kpiId, kpiValue) in element.Kpis)
                {
                    if (elementKpiRules.TryGetValue(kpiId, out var rule))
                    {
                        kpiValue.AssessmentStatus = Assess(kpiValue.Value, rule);
                    }
                    else
                    {
                        kpiValue.AssessmentStatus = KPIAssessmentStatus.Undefined;
                        _logger.LogDebug("KPI {KpiId} set to Undefined for element {ElementId}", kpiId, element.ElementId);
                    }
                }
            }

            if (errors.Any())
            {
                return new ValidationGateResult(false, "Mandatory KPIs missing.", 400, errors);
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

            // Handle case where there is no performance threshold to check
            if (rule.ThresholdRule == null)
            {
                return KPIAssessmentStatus.Pass;
            }

            var threshold = rule.ThresholdRule;
            bool metTarget = false;

            // 2. Performance Threshold Check
            // Use StringComparison instead of ToLower for better performance
            switch (threshold.Type)
            {
                case string t when string.Equals(t, "gte", StringComparison.OrdinalIgnoreCase):
                    metTarget = value >= (threshold.Value ?? threshold.Target ?? 0);
                    break;

                case string t when string.Equals(t, "lte", StringComparison.OrdinalIgnoreCase):
                    metTarget = value <= (threshold.Value ?? threshold.Target ?? 0);
                    break;

                case string t when string.Equals(t, "plus_minus", StringComparison.OrdinalIgnoreCase):
                    if (threshold.Target.HasValue && threshold.Delta.HasValue)
                    {
                        double lowerTarget = threshold.Target.Value - threshold.Delta.Value;
                        double upperTarget = threshold.Target.Value + threshold.Delta.Value;
                        metTarget = value >= lowerTarget && value <= upperTarget;
                    }
                    break;

                case string t when string.Equals(t, "eq", StringComparison.OrdinalIgnoreCase):
                    metTarget = Math.Abs(value - (threshold.Value ?? 0)) < GlobalEpsilon;
                    break;

                default:
                    _logger.LogWarning("Unknown threshold type: {Type} for KPI rule", threshold.Type);
                    metTarget = false;
                    break;
            }

            return metTarget ? KPIAssessmentStatus.Pass : KPIAssessmentStatus.Fail;
        }
    }
}
