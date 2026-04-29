using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class UserStats
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("notificationHistoryCount")]
        public int NotificationHistoryCount { get; set; }
    }
}
