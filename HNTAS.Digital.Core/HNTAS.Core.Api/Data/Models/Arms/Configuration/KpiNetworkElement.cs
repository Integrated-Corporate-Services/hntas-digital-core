using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Configuration
{
    public class KpiNetworkElement
    {
        [JsonPropertyName("type")]
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementType Type { get; set; }

        [JsonPropertyName("kpis")]
        [BsonElement("kpis")]
        public Dictionary<string, KpiRule> Kpis { get; set; } = new();
    }
}
