using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValueRequest
    {
        [JsonPropertyName("value")]
        public required double Value { get; set; }

        [JsonPropertyName("is_kpi_imputed")]
        public bool IsKpiImputed { get; set; } = false;

        [JsonPropertyName("kpi_imputation_details")]
        public string? KpiImputationDetails { get; set; }
    }
}