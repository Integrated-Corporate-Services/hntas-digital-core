using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValue
    {
        [JsonPropertyName("value")]
        [BsonElement("value")]
        public double Value { get; set; }

        [JsonPropertyName("is_kpi_imputed")]
        [BsonElement("isKpiImputed")]
        public bool IsKpiImputed { get; set; } = false;

        [JsonPropertyName("kpi_imputation_details")]
        [BsonElement("kpiImputationDetails")]
        public string? KpiImputationDetails { get; set; }
    }
}
