using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Element
    {
        [BsonElement("networkElementDisplayType")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementDisplayType Type { get; set; }
        [BsonElement("count")]
        [BsonRepresentation(BsonType.Int32)]
        public int? Count { get; set; }
        [BsonElement("address")]
        public RegisteredAddress? Address { get; set; }
        [BsonElement("ecDetails")]
        public ECDetails? ECDetails { get; set; }
        [BsonElement("elementType")]
        public string? ElementType { get; set; }
        [BsonElement("elementId")]
        public string? ElementId { get; set; }
        [BsonElement("soaStages")]
        public List<SoaStages>? SoaStages { get; set; } = [];
    }
}
