using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class ElementSoa : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus Status { get; set; }

        [BsonElement("stages")]
        public List<SoaStages> Stages { get; set; } = [];
    }

    public class SoaStages
    {
        [BsonElement("stage")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage Stage { get; set; }
        [BsonElement("elements")]
        public List<Elements> Elements { get; set; } = [];
    }

    public class Elements
    {
        [BsonElement("networkElementDisplayType")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementDisplayType Type { get; set; }
        [BsonElement("elementType")]
        public string? ElementType { get; set; }
        [BsonElement("elementId")]
        public string? ElementId { get; set; }
        [BsonElement("documents")]
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }
}
