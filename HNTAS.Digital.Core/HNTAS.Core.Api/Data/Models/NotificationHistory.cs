using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;

namespace HNTAS.Core.Api.Data.Models
{
    public class NotificationHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("notificationType")]
        [BsonRepresentation(BsonType.String)]
        public NotificationType NotificationType { get; set; }
        [BsonElement("actorsId")]
        public List<string> ActorsId { get; set; } = [];
        [BsonElement("subject")]
        [BsonRepresentation(BsonType.String)]
        public string Subject { get; set; } = string.Empty;
        [BsonElement("description")]
        [BsonRepresentation(BsonType.String)]
        public string? Description { get; set; }
        [BsonElement("timestamp")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }
        [BsonElement("action")]
        [BsonRepresentation(BsonType.String)]
        public string? Action { get; set; }
        [BsonElement("heatNetworkId")]
        [BsonRepresentation(BsonType.String)]
        public string? HeatNetworkId { get; set; }
        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }
    }

    public enum NotificationType
    {
        RpInvitesNetworkManager = 1,
        NetworkManagerAcceptsInvite,
        NetworkManagerRejectsInvite,
        RpResistersHeatNetwork,
        NetworkManagerResistersHeatNetwork,
        RpInvitesDdhToHeatNetwork,
        NetworkManagerInvitesDdhToHeatNetwork,
        DdhInvitesContributorToHeatNetwork,
        DdhAcceptsInviteToHeatNetwork,
        DdhRejectsInviteToHeatNetwork,
        ContributorAcceptsInviteToHeatNetwork,
        ContributorRejectsInviteToHeatNetwork,
        AssessorAssignsToHeatNetwork,
    }
}
