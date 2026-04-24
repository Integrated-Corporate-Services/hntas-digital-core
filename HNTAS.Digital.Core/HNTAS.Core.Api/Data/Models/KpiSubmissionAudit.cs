using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class KpiSubmissionAudit
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("submissionId")]
        public string SubmissionId { get; set; }

        [BsonElement("networkId")]
        public string NetworkId { get; set; }


        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("sourceSystem")]
        public string SourceSystem { get; set; }

        [BsonElement("periodStart")]
        public string PeriodStart { get; set; }

        [BsonElement("changes")]
        public List<KpiDeltaAudit> Changes { get; set; } = new();
    }
}
