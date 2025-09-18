using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetwork
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("hnId")]
        public string? HnId { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("pathway")]
        public string Pathway { get; set; }

        [BsonElement("soa")]
        public Soa? Soa { get; set; }
    }
}
