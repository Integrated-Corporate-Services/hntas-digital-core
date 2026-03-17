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

        [BsonElement("soaStatus")]
        public string? SoaStatus { get; set; }
        [BsonElement("soaStatusUpdatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? SoaStatusUpdatedAt { get; set; }
        [BsonElement("soaStatusUpdatedBy")]
        public string? SoaStatusUpdatedBy { get; set; }

    }
}
