using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class DocumentBase
    {        

        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("s3Key")]
        public string S3Key { get; set; } = string.Empty;
        [BsonElement("uploadedAt")]
        public DateTime UploadedAt { get; set; }

        [BsonElement("uploadedBy")]
        public string UploadedBy { get; set; } = string.Empty;
    }
}
