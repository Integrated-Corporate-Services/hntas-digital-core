using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class NetworkElement
    {
        [BsonElement("elementId")]
        public required string ElementId { get; set; }

        [JsonPropertyName("type")]
        [BsonElement("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public required HeatNetworkElementType Type { get; set; }

        [JsonPropertyName("kpis")]
        [BsonElement("kpis")]
        public Dictionary<string, KpiValue> Kpis { get; set; } = new();

        [JsonPropertyName("carbon_Calculator_Response")]
        [BsonElement("carbonCalculatorResponse")]
        public CCResponse? CarbonCalculatorResponse { get; set; }
    }

    public class CCResponse
    {
        [JsonPropertyName("total_carbon_emission")]
        [BsonElement("totalCarbonEmission")]
        public decimal TotalCarbonEmission { get; set; }

        [JsonPropertyName("uuid")]
        [BsonElement("uuid")]
        public string Uuid { get; set; } = string.Empty;
    }
}
