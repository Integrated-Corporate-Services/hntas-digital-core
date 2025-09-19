using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class UploadedDocument
    {
        [BsonElement("fileName")]
        public string FileName { get; set; } = null!;

        [BsonElement("s3Key")]
        public string S3Key { get; set; } = null!;

        [BsonElement("phase")]
        [BsonRepresentation(BsonType.String)]
        public SoaPhase Phase { get; set; }

        [BsonElement("stage")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage Stage { get; set; }

        [BsonElement("uploadedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("uploadedBy")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UploadedBy { get; set; } = null!;

    }
}
