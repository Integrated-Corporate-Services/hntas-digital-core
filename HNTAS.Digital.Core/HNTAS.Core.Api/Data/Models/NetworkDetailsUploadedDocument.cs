using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class NetworkDetailsUploadedDocument
    {
        [BsonElement("fileName")]
        public string FileName { get; set; } = null!;

        [BsonElement("s3Key")]
        public string S3Key { get; set; } = null!;        

        [BsonElement("uploadedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UploadedAt { get; set; }

        [BsonElement("uploadedBy")]
        public string UploadedBy { get; set; } = null!;
    }
}
