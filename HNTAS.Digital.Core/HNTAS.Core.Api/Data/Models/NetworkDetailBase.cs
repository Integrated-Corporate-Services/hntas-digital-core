using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class NetworkDetailBase
    {
        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdBy")]
        //[BsonRepresentation(BsonType.ObjectId)]
        public string CreatedBy { get; set; } = null!;

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("updatedBy")]
        //[BsonRepresentation(BsonType.ObjectId)]
        public string? UpdatedBy { get; set; }        
    }
}
