using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class CarbonCalculatorRuleValidation : ICarbonCalculatorRuleValidation
    {
        private readonly IArmsKpiService _armsKpiService;
        private readonly ILogger<CarbonCalculatorRuleValidation> _logger;
        private const double GlobalEpsilon = 1e-6;

        public CarbonCalculatorRuleValidation(IArmsKpiService armsKpiService, ILogger<CarbonCalculatorRuleValidation> logger)
        {
            _armsKpiService = armsKpiService;
            _logger = logger;
        }

        public async Task<ValidationGateResult> ValidateAsync(KpiSubmission dataModel)
        {
            // 1. Run your standard KPI config database lookups here...
            var config = await _armsKpiService.GetConfigurationAsync(dataModel.MetaData.NetworkId);
            if (config == null)
            {
                _logger.LogWarning("KPI Submission failed: No config for Network: {NetworkId}", dataModel.MetaData.NetworkId);
                return new ValidationGateResult(
                     IsValid: false,
                     Message: "Validation Failed",
                     Detail: "KPI Configuration not found for this network.",
                     StatusCode: 404,
                     Errors: new List<KpiSubmissionApiError>
                     {
                    new KpiSubmissionApiError { Code = "CONFIG_NOT_FOUND", Message = "KPI Configuration not found." }
                     });
            }

            var errors = new List<KpiSubmissionApiError>();

            // ==========================================================
            // 2. In-Memory Carbon Calculator Inputs Validation
            // ==========================================================
            if (dataModel.CarbonCalculatorRequest != null)
            {
                // Reference your updated config database sub-property naming path
                var carbonConfig = config.CarbonCalculator;

                if (carbonConfig?.Rules == null || !carbonConfig.Rules.Any())
                {
                    _logger.LogWarning("No Carbon Calculator validation rules found in configuration for Network: {NetworkId}.", dataModel.MetaData.NetworkId);
                    return new ValidationGateResult(
                        IsValid: false,
                        Message: "Validation Failed",
                        Detail: "Carbon Calculator validation rules not found in configuration.",
                        StatusCode: 500,
                        Errors: new List<KpiSubmissionApiError>
                        {
                    new KpiSubmissionApiError { Code = "CONFIGURATION_ERROR", Message = "Carbon Calculator validation rules are missing in the configuration." }
                        });
                }

                var invalidCarbonKpis = new List<string>();

                foreach (var section in dataModel.CarbonCalculatorRequest.Values)
                {
                    if (section == null) continue;

                    foreach (var (dataId, dataValue) in section)
                    {
                        // Pull validation logic rules from the database configurations dictionary mapping
                        if (carbonConfig.Rules.TryGetValue(dataId, out var rule) && rule != null)
                        {
                            // Clean handling for BsonValues: convert numeric types or strings to double safely
                            if (dataValue?.Value != null && BsonConversionHelper.TryGetDouble(dataValue.Value, out var numericValue))
                            {
                                var status = Assess(numericValue, rule);

                                if (status == KPIAssessmentStatus.Fail || status == KPIAssessmentStatus.OutsideLimit)
                                {
                                    invalidCarbonKpis.Add(dataId);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Carbon input value for {DataId} could not be parsed as a number.", dataId);
                                invalidCarbonKpis.Add(dataId);
                            }
                        }
                    }
                }

                if (invalidCarbonKpis.Any())
                {
                    errors.Add(new KpiSubmissionApiError
                    {
                        Code = "INVALID_CARBON_INPUT_VALUE",
                        Message = "One or more carbon calculator inputs are outside allowable threshold tolerances.",
                        Kpis = invalidCarbonKpis
                    });
                }
            }

            // 3. Process Core Operational Element Validation Loops Here...

            if (errors.Any())
            {
                return new ValidationGateResult(false, "Validation Failed") { Errors = errors, StatusCode = 400 };
            }

            return new ValidationGateResult(true);
        }

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
