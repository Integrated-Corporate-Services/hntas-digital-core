using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [ExcludeFromCodeCoverage]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValue
    {
        [JsonPropertyName("value")]
        [BsonElement("value")]
        public double Value { get; set; }

        [JsonPropertyName("assessment_status")]
        [BsonElement("assessmentStatus")]
        [BsonRepresentation(BsonType.String)]
        public KPIAssessmentStatus AssessmentStatus { get; set; }

        [JsonPropertyName("is_kpi_imputed")]
        [BsonElement("isKpiImputed")]
        public bool IsKpiImputed { get; set; } = false;

        [JsonPropertyName("kpi_imputation_details")]
        [BsonElement("kpiImputationDetails")]
        public string? KpiImputationDetails { get; set; }
    }
}
