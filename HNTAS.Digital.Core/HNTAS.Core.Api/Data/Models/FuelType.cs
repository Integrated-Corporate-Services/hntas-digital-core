using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class FuelType
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("fuelId")]
        public int FuelId { get; set; }
        [BsonElement("type")]
        public string Type { get; set; } = null!;
    }
}
