using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    //public class ElementSoa : NetworkDetailBase
    //{
    //    [BsonElement("status")]
    //    [BsonRepresentation(BsonType.String)]
    //    public NetworkDetailsStatus Status { get; set; }

    //    [BsonElement("elements")]
    //    public List<Elements> Elements { get; set; } = [];
        
    //}    

    //public class Elements
    //{
    //    [BsonElement("elementId")]
    //    public string? ElementId { get; set; }

    //    //[BsonElement("stages")]
    //    //public List<SoaStages> Stages { get; set; } = [];        
    //}

    public class SoaStages
    {
        [BsonElement("stageId")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage? StageId { get; set; }
        [BsonElement("document")]
        public NetworkDetailsUploadedDocument? Document { get; set; }

    }
}
