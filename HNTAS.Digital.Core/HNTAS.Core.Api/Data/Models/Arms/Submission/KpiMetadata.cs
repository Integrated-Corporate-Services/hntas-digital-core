using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiMetadata
    {
        [JsonPropertyName("network_id")]
        [BsonElement("networkId")]
        public required string NetworkId { get; set; }

        [JsonPropertyName("period_start")]
        [BsonElement("periodStart")]
        public required string PeriodStart { get; set; }

        [JsonPropertyName("source_system")]
        [BsonElement("sourceSystem")]
        public string? SourceSystem { get; set; }

        [JsonPropertyName("additional_details")]
        [BsonElement("additionalDetails")]
        public string? AdditionalDetails { get; set; }
    }
}
