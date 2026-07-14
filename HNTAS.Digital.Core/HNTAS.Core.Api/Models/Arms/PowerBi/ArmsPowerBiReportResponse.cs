using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.PowerBi
{
    [ExcludeFromCodeCoverage]
    public class ArmsPowerBiReportResponse
    {
        [JsonPropertyName("hn_id")]
        public string HnId { get; set; } = null!;

        [JsonPropertyName("org_id")]
        public string OrgId { get; set; } = null!;

        [JsonPropertyName("period_start")]
        public string PeriodStart { get; set; } = null!;

        [JsonPropertyName("element_id")]
        public string? ElementId { get; set; }

        [JsonPropertyName("element_type")]
        public string ElementType { get; set; } = null!;

        [JsonPropertyName("kpi_id")]
        public string KpiId { get; set; } = null!;

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("assessment_status")]
        public string AssessmentStatus { get; set; } = null!;
    }
}
