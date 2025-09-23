using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class NetworkTypeSelection
    {
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkType Type { get; set; }

        [BsonElement("otherNetworkDescription")]
        public string? OtherNetworkDescription { get; set; }
    }
}
