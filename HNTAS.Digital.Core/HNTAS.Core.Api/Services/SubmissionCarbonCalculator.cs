using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms.V2;
using MongoDB.Bson;

namespace HNTAS.Core.Api.Services
{
    public class SubmissionCarbonCalculator : ISubmissionCarbonCalculator
    {
        private readonly ICarbonCalculatorService _ccService;
        private readonly ILogger<SubmissionCarbonCalculator> _logger;
        private readonly IArmsKpiService _kpiService;
        private readonly IHeatNetworkService _heatNetworkService;

        public SubmissionCarbonCalculator(ICarbonCalculatorService ccService, ILogger<SubmissionCarbonCalculator> logger, IArmsKpiService kpiService, IHeatNetworkService heatNetworkService)
        {
            _ccService = ccService;
            _logger = logger;
            _kpiService = kpiService;
            _heatNetworkService = heatNetworkService;
        }

        public async Task ProcessCarbonCalculationsAsync(KpiSubmissionRequestV2 request, KpiSubmission dataModel)
        {

            // 1. Check if there is at least one EnergyCentre element before doing any work
            var energyCentreElement = request.Elements.FirstOrDefault(e => e.Type == HeatNetworkElementType.EnergyCentre.ToString());
            if (energyCentreElement == null)
            {
                return; // No Energy Centre found, skip carbon calculation completely
            }

            // 2. Fetch configuration and verify defaults exist
            var config = await _kpiService.GetConfigurationAsync(request.MetaData.NetworkId);
            var configDefaults = config?.CarbonCalculator?.Defaults ?? new Dictionary<string, BsonValue>();

            if (configDefaults == null || !configDefaults.Any())
            {
                _logger.LogWarning("No Carbon Calculator defaults found in configuration for Network: {NetworkId}. Skipping calculation.", request.MetaData.NetworkId);
                return;
            }

            if (!DateTime.TryParse(request.MetaData.PeriodStart, out DateTime currentPeriod))
            {
                _logger.LogError("Invalid PeriodStart format: {PeriodStart}. Cannot calculate rolling carbon metrics.", request.MetaData.PeriodStart);
                return;
            }

            int currentMonth = currentPeriod.Month;
            int currentYear = currentPeriod.Year;

            var inputs = request.CarbonInputsV2;

            // Extract sections safely
            inputs.TryGetValue("mata_data", out var backgroundSection);
            inputs.TryGetValue("chp_totals", out var chpSection);
            inputs.TryGetValue("hpm_totals", out var hpmSection);
            inputs.TryGetValue("blr_totals", out var blrSection);

            double currentEc47 = chpSection != null && chpSection.TryGetValue("EC-DATA-47", out var kpi47) ? kpi47.AsDouble() : 0;
            int currentEc53 = chpSection != null && chpSection.TryGetValue("EC-DATA-53", out var kpi53) ? kpi53.AsInt() : 0;
            int currentEc55 = chpSection != null && chpSection.TryGetValue("EC-DATA-55", out var kpi55) ? kpi55.AsInt() : 0;
            int currentEc57 = chpSection != null && chpSection.TryGetValue("EC-DATA-57", out var kpi57) ? kpi57.AsInt() : 0;

            int currentEc66 = hpmSection != null && hpmSection.TryGetValue("EC-DATA-66", out var kpi66) ? kpi66.AsInt() : 0;
            int currentEc68 = hpmSection != null && hpmSection.TryGetValue("EC-DATA-68", out var kpi68) ? kpi68.AsInt() : 0;

            int currentEc84 = blrSection != null && blrSection.TryGetValue("EC-DATA-84", out var kpi84) ? kpi84.AsInt() : 0;
            int currentEc86 = blrSection != null && blrSection.TryGetValue("EC-DATA-86", out var kpi86) ? kpi86.AsInt() : 0;


            // Initialize accumulation metrics with current month values
            double totalEc47 = currentEc47;
            int totalEc53 = currentEc53;
            int totalEc55 = currentEc55;
            int totalEc57 = currentEc57;
            int totalEc66 = currentEc66;
            int totalEc68 = currentEc68;
            int totalEc84 = currentEc84;
            int totalEc86 = currentEc86;

            int submittedMonthsCount = 1;

            if (currentMonth > 1)
            {
                // Replace this method call with your dataModel repository layer service lookup
                List<KpiSubmission> historicalSubmissions = await _kpiService.GetSubmissionsForYearAsync(request.MetaData.NetworkId, currentYear);

                foreach (var history in historicalSubmissions)
                {
                    if (DateTime.TryParse(history.MetaData.PeriodStart, out DateTime historyDate) && historyDate.Month < currentMonth)
                    {
                        var histInputs = history.CarbonCalculatorInputs;
                        if (histInputs == null) continue;

                        histInputs.TryGetValue("chp_totals", out var hChp);
                        histInputs.TryGetValue("hpm_totals", out var hHpm);
                        histInputs.TryGetValue("blr_totals", out var hBlr);

                        totalEc47 += hChp != null && hChp.TryGetValue("EC-DATA-47", out var hk47) ? hk47.Value.AsInt32 : 0.0;
                        totalEc53 += hChp != null && hChp.TryGetValue("EC-DATA-53", out var hk53) ? hk53.Value.AsInt32 : 0;
                        totalEc55 += hChp != null && hChp.TryGetValue("EC-DATA-55", out var hk55) ? hk55.Value.AsInt32 : 0;
                        totalEc57 += hChp != null && hChp.TryGetValue("EC-DATA-57", out var hk57) ? hk57.Value.AsInt32 : 0;

                        totalEc66 += hHpm != null && hHpm.TryGetValue("EC-DATA-66", out var hk66) ? hk66.Value.AsInt32 : 0;
                        totalEc68 += hHpm != null && hHpm.TryGetValue("EC-DATA-68", out var hk68) ? hk68.Value.AsInt32 : 0;

                        totalEc84 += hBlr != null && hBlr.TryGetValue("EC-DATA-84", out var hk84) ? hk84.Value.AsInt32 : 0;
                        totalEc86 += hBlr != null && hBlr.TryGetValue("EC-DATA-86", out var hk86) ? hk86.Value.AsInt32 : 0;

                        // Increment the counter for every valid historical month found
                        submittedMonthsCount++;
                    }
                }
            }

            // 6. Calculate the annualization factor multiplier (e.g., if Feb, Month=2, Multiplier = 12 / 2 = 6)
            double projectionMultiplier = 12.0 / submittedMonthsCount;

            decimal projectedEc47 = (decimal)Math.Round(totalEc47 * projectionMultiplier);
            int projectedEc53 = (int)Math.Round(totalEc53 * projectionMultiplier);
            int projectedEc55 = (int)Math.Round(totalEc55 * projectionMultiplier);
            int projectedEc57 = (int)Math.Round(totalEc57 * projectionMultiplier);
            int projectedEc66 = (int)Math.Round(totalEc66 * projectionMultiplier);
            int projectedEc68 = (int)Math.Round(totalEc68 * projectionMultiplier);
            int projectedEc84 = (int)Math.Round(totalEc84 * projectionMultiplier);
            int projectedEc86 = (int)Math.Round(totalEc86 * projectionMultiplier);

            var heatNetwork = await _heatNetworkService.GetByHnIdAsync(request.MetaData.NetworkId.ToUpper());

            var requestModel = new CarbonCalculatorRequest
            {
                Background = new Background
                {
                    // Mandatory field
                    DateWorkbookCompleted = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    NetworkStatus = configDefaults["EC-DATA-20"].ToString(),
                    NetworkServiceProvision = configDefaults["EC-DATA-21"].ToString(),
                    Name = $"{heatNetwork.Name} ({request.MetaData.NetworkId}) - Period: {request.MetaData.PeriodStart}",
                    NetworkID = request.MetaData.NetworkId,
                    NetworkName = heatNetwork.Name,
                    PostcodeOfThePrimaryEnergyCentre = configDefaults["EC-DATA-28"].ToString(),
                    ContactEmail = configDefaults["EC-DATA-32"].ToString(),
                    CommissioningDate = configDefaults["EC-DATA-35"].ToString()
                },
                Energy = new Energy
                {
                    YearCount = configDefaults["EC-DATA-36"].AsInt32,
                    StartYear = configDefaults["EC-DATA-37"].AsInt32,
                    ChpCount = 1,
                    EnergyHeatNetworkPrimaryLosses = [configDefaults["EC-DATA-38"].AsInt32],
                    ChpInputs = new List<ChpInput>
                            {
                                new ChpInput
                                {
                                    ChpFuelTypeInput = configDefaults["EC-DATA-50"].AsInt32,
                                    ChpOperationalModeInput = configDefaults["EC-DATA-51"].ToString(),
                                    ChpInstallationDateInput = chpSection != null && chpSection.TryGetValue("EC-DATA-52", out var kpi51)
                                                            ? kpi51.Value.ToString()
                                                            : null,
                                    ChpUsefulHeatValue = [projectedEc53],
                                    ChpElectricityGeneratedValue = [projectedEc55],
                                    ChpFuelUsedValue = [projectedEc57],
                                    ChpHeatCoolingValue = [configDefaults["EC-DATA-59"].AsInt32],
                                    ChpSleevingPCentValue = [configDefaults["EC-DATA-61"].AsInt32],
                                    ChpMaxHeatOutput = configDefaults["EC-DATA-63"].AsInt32,
                                    ChpMaxElectricityOutput = configDefaults["EC-DATA-64"].AsInt32,
                                }
                            },
                    EppElectricityUsedForPumpingValue = [projectedEc47],
                    BoilerCount = blrSection == null ? 0 : 1,
                    BoilerInputs = blrSection == null ? new List<BoilerInput>() : new List<BoilerInput>
                            {
                               new BoilerInput
                               {
                                   BlrTypeFuelUsedInput = configDefaults["EC-DATA-83"].AsInt32,
                                   BlrUsefulHeatGeneratedValue = [projectedEc84],
                                   BlrFuelUsedByValue = [projectedEc86],
                                   BlrHeatUsedForCoolingProductionValue = [configDefaults["EC-DATA-88"].AsInt32],
                                   BlrSleevingPCentValue = [configDefaults["EC-DATA-90"].AsInt32],
                                   BlrMaxHeatOutput = configDefaults["EC-DATA-92"].AsInt32,
                               }
                            },
                    RecoveredCount = 0,
                    RecoveredInputs = new List<RecoveredInput>(),
                    HeatPumpCount = hpmSection == null ? 0 : 1,
                    HeatPumpInputs = hpmSection == null ? new List<HeatPumpInput>() : new List<HeatPumpInput>
                            {
                                new HeatPumpInput {
                                    HpmTypeFuelUsedInput = configDefaults["EC-DATA-65"].AsInt32,
                                    HpmUsefulHeatGeneratedValue = [projectedEc66],
                                    HpmEnergyUsedValue = [projectedEc68],
                                    HpmUsefulCoolingGeneratedValue = [configDefaults["EC-DATA-70"].AsInt32],
                                    HpmSleevingPCentValue = [configDefaults["EC-DATA-72"].AsInt32],
                                    HpmMaxHeatOutput = configDefaults["EC-DATA-74"].AsInt32,
                                }
                            }
                }
            };

            // Create carbon calculator inputs for backwards compatibility
            var cc_result = await _ccService.RunAsync(requestModel);

            dataModel.CarbonCalculatorResponse = new Data.Models.Arms.Submission.CarbonCalculatorResponse
            {
                TotalCarbonEmission = (decimal)(cc_result?.TotalCarbonEmission),
                Uuid = cc_result?.Uuid
            };
        }
    }
}
