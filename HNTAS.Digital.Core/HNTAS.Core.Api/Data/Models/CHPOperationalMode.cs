using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class CHPOperationalMode
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("chpId")]
        public int ChpId { get; set; }
        [BsonElement("ident")]
        public string Ident { get; set; } = null!;
        [BsonElement("operationalMode")]
        public string OperationalMode { get; set; } = null!;
    }
}
