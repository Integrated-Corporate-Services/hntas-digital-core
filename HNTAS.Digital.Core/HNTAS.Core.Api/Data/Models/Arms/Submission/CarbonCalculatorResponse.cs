using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    public class CarbonCalculatorResponse
    {
        [JsonPropertyName("total_carbon_emission")]
        [BsonElement("totalCarbonEmission")]
        public decimal TotalCarbonEmission { get; set; }

        [JsonPropertyName("uuid")]
        [BsonElement("uuid")]
        public string Uuid { get; set; } = string.Empty;
    }
}
