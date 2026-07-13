using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
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
