using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HnCarbonCalculation
    {

        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("hnId")]
        public string HnId { get; set; } = null!;

        [BsonElement("uuid")]
        public string Uuid { get; set; } = null!;

        [BsonElement("totalCarbonEmission")]
        public decimal TotalCarbonEmission { get; set; }

        [BsonElement("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    }
}
