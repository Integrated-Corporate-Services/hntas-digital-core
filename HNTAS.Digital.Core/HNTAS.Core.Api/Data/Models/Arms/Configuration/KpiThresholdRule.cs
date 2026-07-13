using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Configuration
{
    [ExcludeFromCodeCoverage]
    public class KpiThresholdRule
    {
        [JsonPropertyName("type")]
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty; // e.g., "gte", "lte", "plus_minus"

        [JsonPropertyName("value")]
        [BsonElement("value")]
        [BsonIgnoreIfNull]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Value { get; set; }

        [JsonPropertyName("target")]
        [BsonElement("target")]
        [BsonIgnoreIfNull]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Target { get; set; }

        [JsonPropertyName("delta")]
        [BsonElement("delta")]
        [BsonIgnoreIfNull]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Delta { get; set; }
    }
}
