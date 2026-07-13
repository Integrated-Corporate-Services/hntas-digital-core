using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Configuration
{
    [ExcludeFromCodeCoverage]
    public class KpiConfiguration
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        [JsonPropertyName("network_id")]
        [BsonElement("networkId")]
        public string NetworkId { get; set; }

        [JsonPropertyName("elements")]
        [BsonElement("elements")]
        public List<KpiNetworkElement> Elements { get; set; } = new();

        [JsonPropertyName("carbon_calculator")]
        [BsonElement("carbonCalculator")]
        [BsonIgnoreIfNull]
        public CarbonCalculatorConfig? CarbonCalculator { get; set; } = new();

        [JsonPropertyName("created_at")]
        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedAt { get; set; }
    }

    public class CarbonCalculatorConfig
    {
        // Stores the EC-DATA threshold boundaries (EC-DATA-53 to 86 limits)
        // Maps to your database representation of a KPI validation rule document structure
        [JsonPropertyName("rules")]
        [BsonElement("Rules")]
        [BsonIgnoreIfNull]
        public Dictionary<string, KpiRule>? Rules { get; set; } = new();

        // Stores the static string/int defaults (EC-DATA-20, EC-DATA-32 etc.)
        [JsonPropertyName("defaults")]
        [BsonElement("defaults")]
        [BsonIgnoreIfNull]
        public Dictionary<string, BsonValue>? Defaults { get; set; } = new();
    }
}
