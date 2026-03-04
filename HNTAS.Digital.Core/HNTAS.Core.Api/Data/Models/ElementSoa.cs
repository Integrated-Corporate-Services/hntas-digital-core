using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaStages
    {
        [BsonElement("stageId")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage? StageId { get; set; }
        [BsonElement("document")]
        public NetworkDetailsUploadedDocument? Document { get; set; }

    }
}
