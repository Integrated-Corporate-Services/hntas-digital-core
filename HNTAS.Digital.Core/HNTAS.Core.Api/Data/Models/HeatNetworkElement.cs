using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetworkElement
    {
        [BsonElement("name")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementType Name { get; set; }

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("locations")]
        public List<string> Locations { get; set; } = [];

        [BsonElement("documents")]
        public List<UploadedDocument> Documents { get; set; } = [];
    }
}
