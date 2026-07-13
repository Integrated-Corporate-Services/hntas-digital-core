using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class CarbonCalculatorRequest
    {
        [JsonPropertyName("background")]
        public Background Background { get; set; } = new();

        [JsonPropertyName("energy")]
        public Energy Energy { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class Background
    {
        [JsonPropertyName("dateWorkbookCompleted")] public string? DateWorkbookCompleted { get; set; }
        [JsonPropertyName("networkStatus")] public string? NetworkStatus { get; set; }
        [JsonPropertyName("networkServiceProvision")] public string? NetworkServiceProvision { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("networkID")] public string? NetworkID { get; set; }
        [JsonPropertyName("networkName")] public string? NetworkName { get; set; }
        [JsonPropertyName("networkNamePrevious")] public string? NetworkNamePrevious { get; set; }
        [JsonPropertyName("heatNetworkZone")] public string? HeatNetworkZone { get; set; }
        [JsonPropertyName("addressOfThePrimaryEnergyCentre")] public string? AddressOfThePrimaryEnergyCentre { get; set; }
        [JsonPropertyName("postcodeOfThePrimaryEnergyCentre")] public string? PostcodeOfThePrimaryEnergyCentre { get; set; }
        [JsonPropertyName("networkOperator")] public string? NetworkOperator { get; set; }
        [JsonPropertyName("networkOperatorContact")] public string? NetworkOperatorContact { get; set; }
        [JsonPropertyName("contactTelephone")] public string? ContactTelephone { get; set; }
        [JsonPropertyName("contactEmail")] public string? ContactEmail { get; set; }
        [JsonPropertyName("descriptionOfNetwork")] public string? DescriptionOfNetwork { get; set; }
        [JsonPropertyName("dateOfInitialOperation")] public string? DateOfInitialOperation { get; set; }
        [JsonPropertyName("commissioningDate")] public string? CommissioningDate { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Energy
    {
        [JsonPropertyName("yearCount")] public int? YearCount { get; set; }
        [JsonPropertyName("startYear")] public int? StartYear { get; set; }

        // NUMBER arrays (decimal)
        [JsonPropertyName("energyHeatNetworkPrimaryLosses")]
        public List<decimal>? EnergyHeatNetworkPrimaryLosses { get; set; }

        // Plant counts
        [JsonPropertyName("chpCount")] public int? ChpCount { get; set; }
        [JsonPropertyName("heatPumpCount")] public int? HeatPumpCount { get; set; }
        [JsonPropertyName("recoveredCount")] public int? RecoveredCount { get; set; }
        [JsonPropertyName("boilerCount")] public int? BoilerCount { get; set; }

        // Inputs (Lists of objects)
        [JsonPropertyName("chpInputs")] public List<ChpInput>? ChpInputs { get; set; }
        [JsonPropertyName("heatPumpInputs")] public List<HeatPumpInput>? HeatPumpInputs { get; set; }
        [JsonPropertyName("recoveredInputs")] public List<RecoveredInput>? RecoveredInputs { get; set; }
        [JsonPropertyName("boilerInputs")] public List<BoilerInput>? BoilerInputs { get; set; }

        // Optional arrays/notes
        [JsonPropertyName("eppElectricityUsedForPumpingValue")] public List<decimal>? EppElectricityUsedForPumpingValue { get; set; }
        [JsonPropertyName("eppElectricityUsedForPumpingNotes")] public string? EppElectricityUsedForPumpingNotes { get; set; }
        [JsonPropertyName("eppSleevingPCentNotes")] public string? EppSleevingPCentNotes { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ChpInput
    {
        [JsonPropertyName("chpFuelTypeInput")] public int? ChpFuelTypeInput { get; set; }
        [JsonPropertyName("chpInstallationDateInput")] public string? ChpInstallationDateInput { get; set; }
        [JsonPropertyName("chpOperationalModeInput")] public string? ChpOperationalModeInput { get; set; }

        [JsonPropertyName("chpUsefulHeatValue")] public List<int>? ChpUsefulHeatValue { get; set; }
        [JsonPropertyName("chpUsefulHeatNotes")] public string? ChpUsefulHeatNotes { get; set; }

        [JsonPropertyName("chpElectricityGeneratedValue")] public List<int>? ChpElectricityGeneratedValue { get; set; }
        [JsonPropertyName("chpElectricityGeneratedNotes")] public string? ChpElectricityGeneratedNotes { get; set; }

        [JsonPropertyName("chpFuelUsedValue")] public List<int>? ChpFuelUsedValue { get; set; }
        [JsonPropertyName("chpFuelUsedNotes")] public string? ChpFuelUsedNotes { get; set; }

        [JsonPropertyName("chpHeatCoolingValue")] public List<int>? ChpHeatCoolingValue { get; set; }
        [JsonPropertyName("chpHeatCoolingNotes")] public string? ChpHeatCoolingNotes { get; set; }

        [JsonPropertyName("chpSleevingPCentValue")] public List<int>? ChpSleevingPCentValue { get; set; }
        [JsonPropertyName("chpSleevingPCentNotes")] public string? ChpSleevingPCentNotes { get; set; }

        [JsonPropertyName("chpMaxHeatOutput")] public int? ChpMaxHeatOutput { get; set; }
        [JsonPropertyName("chpMaxElectricityOutput")] public int? ChpMaxElectricityOutput { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class HeatPumpInput
    {
        [JsonPropertyName("hpmTypeFuelUsedInput")] public int? HpmTypeFuelUsedInput { get; set; }

        [JsonPropertyName("hpmUsefulHeatGeneratedValue")] public List<int>? HpmUsefulHeatGeneratedValue { get; set; }
        [JsonPropertyName("hpmUsefulHeatGeneratedNotes")] public string? HpmUsefulHeatGeneratedNotes { get; set; }

        [JsonPropertyName("hpmEnergyUsedValue")] public List<int>? HpmEnergyUsedValue { get; set; }
        [JsonPropertyName("hpmEnergyUsedNotes")] public string? HpmEnergyUsedNotes { get; set; }

        [JsonPropertyName("hpmUsefulCoolingGeneratedValue")] public List<int>? HpmUsefulCoolingGeneratedValue { get; set; }
        [JsonPropertyName("hpmUsefulCoolingGeneratedNotes")] public string? HpmUsefulCoolingGeneratedNotes { get; set; }

        [JsonPropertyName("hpmSleevingPCentValue")] public List<int>? HpmSleevingPCentValue { get; set; }
        [JsonPropertyName("hpmSleevingPCentNotes")] public string? HpmSleevingPCentNotes { get; set; }

        [JsonPropertyName("hpmMaxHeatOutput")] public int? HpmMaxHeatOutput { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public sealed class RecoveredInput
    {
        [JsonPropertyName("hrwHeatRecoverySourceInput")] public int? HrwHeatRecoverySourceInput { get; set; }

        [JsonPropertyName("hrwUsefulHeatGeneratedValue")] public List<int>? HrwUsefulHeatGeneratedValue { get; set; }
        [JsonPropertyName("hrwUsefulHeatGeneratedNotes")] public string? HrwUsefulHeatGeneratedNotes { get; set; }

        [JsonPropertyName("hrwHeatUsedByCoolingProductionValue")] public List<int>? HrwHeatUsedByCoolingProductionValue { get; set; }
        [JsonPropertyName("hrwHeatUsedByCoolingProductionNotes")] public string? HrwHeatUsedByCoolingProductionNotes { get; set; }

        [JsonPropertyName("hrwSleevingPCentValue")] public List<int>? HrwSleevingPCentValue { get; set; }
        [JsonPropertyName("hrwSleevingPCentNotes")] public string? HrwSleevingPCentNotes { get; set; }

        [JsonPropertyName("hrwMaxHeatOutput")] public int? HrwMaxHeatOutput { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public sealed class BoilerInput
    {
        [JsonPropertyName("blrTypeFuelUsedInput")] public int? BlrTypeFuelUsedInput { get; set; }

        [JsonPropertyName("blrUsefulHeatGeneratedValue")] public List<int>? BlrUsefulHeatGeneratedValue { get; set; }
        [JsonPropertyName("blrUsefulHeatGeneratedNotes")] public string? BlrUsefulHeatGeneratedNotes { get; set; }

        [JsonPropertyName("blrFuelUsedByValue")] public List<int>? BlrFuelUsedByValue { get; set; }
        [JsonPropertyName("blrFuelUsedByNotes")] public string? BlrFuelUsedByNotes { get; set; }

        [JsonPropertyName("blrHeatUsedForCoolingProductionValue")] public List<int>? BlrHeatUsedForCoolingProductionValue { get; set; }
        [JsonPropertyName("blrHeatUsedForCoolingProductionNotes")] public string? BlrHeatUsedForCoolingProductionNotes { get; set; }

        [JsonPropertyName("blrSleevingPCentValue")] public List<int>? BlrSleevingPCentValue { get; set; }
        [JsonPropertyName("blrSleevingPCentNotes")] public string? BlrSleevingPCentNotes { get; set; }

        [JsonPropertyName("blrMaxHeatOutput")] public int? BlrMaxHeatOutput { get; set; }
    }
}