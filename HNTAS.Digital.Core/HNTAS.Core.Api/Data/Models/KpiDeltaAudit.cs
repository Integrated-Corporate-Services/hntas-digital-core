using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class KpiDeltaAudit
    {
        [BsonElement("elementId")]
        [BsonIgnoreIfNull]
        public string ElementId { get; set; }

        [BsonElement("aggregated")]
        public bool Aggregated { get; set; }

        [BsonElement("kpiId")]
        public string KpiId { get; set; }

        [BsonElement("property")]
        public string Property { get; set; }

        [BsonElement("old")]
        public object Old { get; set; }

        [BsonElement("new")]
        public object New { get; set; }
    }
}
