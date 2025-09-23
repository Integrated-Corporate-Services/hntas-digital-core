using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models
{
    public class HeatNetworkUserResponse
    {
        [BsonElement("hnId")]
        public string HnId { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("location")]
        public string Location { get; set; }
    }
}
