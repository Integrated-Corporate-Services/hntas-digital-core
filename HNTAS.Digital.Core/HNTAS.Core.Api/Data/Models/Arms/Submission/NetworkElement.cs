using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [ExcludeFromCodeCoverage]
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

    }
}
