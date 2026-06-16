using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Document : DocumentBase
    {
        [BsonElement("phase")]
        [BsonRepresentation(BsonType.String)]
        public SoaPhase Phase { get; set; }

        [BsonElement("stage")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage? Stage { get; set; }
        
    }
}
