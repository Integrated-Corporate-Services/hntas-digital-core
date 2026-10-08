using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class NotificationStats
    {
        [BsonElement("notificationHistoryCount")]
        public int NotificationHistoryCount { get; set; }
        [BsonElement("lastVisitedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastVisitedAt { get; set; }
    }
}
