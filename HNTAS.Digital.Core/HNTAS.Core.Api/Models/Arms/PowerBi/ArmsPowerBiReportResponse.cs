using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.PowerBi
{
    [ExcludeFromCodeCoverage]
    public class ArmsPowerBiReportResponse
    {
        [JsonPropertyName("hnId")]
        public string HnId { get; set; } = null!;

        [JsonPropertyName("orgId")]
        public string OrgId { get; set; } = null!;

        [JsonPropertyName("periodStart")]
        public string PeriodStart { get; set; } = null!;

        [JsonPropertyName("elementId")]
        public string? ElementId { get; set; }

        [JsonPropertyName("elementType")]
        public string ElementType { get; set; } = null!;

        [JsonPropertyName("kpiId")]
        public string KpiId { get; set; } = null!;

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("assessmentStatus")]
        public string AssessmentStatus { get; set; } = null!;
    }
}
