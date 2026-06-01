using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.V2
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiSubmissionRequestV2 : BaseKpiSubmissionRequest
    {
        [JsonPropertyOrder(3)]
        [JsonPropertyName("elements")]
        public List<NetworkElementRequestV2> Elements { get; set; } = new();
    }

    public class NetworkElementRequestV2 : NetworkElementRequest
    {
        /// <summary>
        /// Dedicated block for Carbon Calculator fields on EnergyCentre types.
        /// Organised into sectioned blocks (e.g., "metadata", "energy_totals") where 
        /// each KPI key contains a nested metadata object wrapper (value, is_kpi_imputed, kpi_imputation_details).
        /// </summary>
        [JsonPropertyOrder(4)]
        [JsonPropertyName("carbon_calculator_inputs")]
        public Dictionary<string, Dictionary<string, CCKpiValueRequest>>? CarbonInputsV2 { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CCKpiValueRequest
    {
        [JsonPropertyName("value")]
        public required object Value { get; set; }

        [JsonPropertyName("is_kpi_imputed")]
        public bool IsKpiImputed { get; set; } = false;

        [JsonPropertyName("kpi_imputation_details")]
        public string? KpiImputationDetails { get; set; }
    }
}
