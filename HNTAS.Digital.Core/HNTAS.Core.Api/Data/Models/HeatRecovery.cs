using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatRecovery
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("heatRecoveryId")]
        public int HeatRecoveryId { get; set; }
        [BsonElement("ident")]
        public string Ident { get; set; } = null!;
        [BsonElement("heatRecoverySource")]
        public string HeatRecoverySource { get; set; } = null!;
    }
}
