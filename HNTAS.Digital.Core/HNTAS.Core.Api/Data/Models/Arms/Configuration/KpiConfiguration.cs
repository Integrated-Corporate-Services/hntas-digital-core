using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Configuration
{
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

        [JsonPropertyName("carbon_calculator_defaults")]
        [BsonElement("carbonCalculatorDefaults")]
        public Dictionary<string, object>? CarbonCalculatorDefaults { get; set; } = new();

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
}
