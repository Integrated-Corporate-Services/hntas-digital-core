using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models
{
    public class ManagedUserResponse
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("emailId")]
        public string EmailId { get; set; } = null!;

        [BsonElement("status")]
        public string Status { get; set; } = null!;

        [BsonElement("heatNetworks")]
        public List<HeatNetworkInfo>? HeatNetworks { get; set; }

        [BsonElement("roles")]
        public List<string>? Roles { get; set; }
    }

    public class HeatNetworkInfo
    {
        [BsonElement("hnId")]
        public string HnId { get; set; } = null!;

        [BsonElement("name")]
        public string Name { get; set; } = null!;
    }
}
