using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    public class KpiSubmission
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("submission_id")]
        public string? SubmissionId { get; set; }

        [BsonElement("metaData")]
        [JsonPropertyName("meta_data")]
        public required KpiMetadata MetaData { get; set; }

        [BsonElement("consumerConnectionAggregatedKpis")]
        [JsonPropertyName("consumer_connection_aggregated_kpis")]
        public Dictionary<string, KpiValueAggregated>? ConsumerConnectionAggregatedKpis { get; set; }

        [JsonPropertyName("elements")]
        [BsonElement("elements")]
        public List<NetworkElement> Elements { get; set; } = new();

        [JsonPropertyName("created_at")]
        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
    }
}
