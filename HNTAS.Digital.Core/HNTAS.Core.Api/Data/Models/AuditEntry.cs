using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class AuditEntry<T>
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("entryType")]
        public string EntryType { get; set; } // e.g., "HeatNetworkCharacteristicsUpdated"

        [BsonElement("entityId")]
        public string EntityId { get; set; } // The HnId

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonElement("timestamp")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("before")]
        public T? Before { get; set; } // Full snapshot of HeatNetwork before change

        [BsonElement("after")]
        public T? After { get; set; } // Full snapshot of HeatNetwork after change

        [BsonElement("changeNote")]
        public string? ChangeNote { get; set; }        

        [BsonElement("elementName")]
        public string? ElementName { get; set; }

        [BsonElement("phase")]
        public string? Phase { get; set; }

        [BsonElement("stage")]
        public string? Stage { get; set; }
    }
}
