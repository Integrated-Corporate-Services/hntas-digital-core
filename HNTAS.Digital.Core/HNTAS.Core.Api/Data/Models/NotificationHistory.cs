using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;

namespace HNTAS.Core.Api.Data.Models
{
    public class NotificationHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("notificationType")]
        [BsonRepresentation(BsonType.String)]
        public NotificationHistoryType NotificationType { get; set; }
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
        [BsonElement("eligibleRoles")]
        public List<string> EligibleRoles { get; set; } = [];
        [BsonElement("heatNetworkId")]
        [BsonRepresentation(BsonType.String)]
        public string? HeatNetworkId { get; set; }
        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }
    }    

    public static class NotificationHistoryActions
    {        
        public const string NetworkManagers = "Network managers";
        public const string DDHAndContributors = "DDH and contributors";
        public const string HeatNetworkDetails = "Heat network details";
    }

    public static class NotificationHistorySubjects
    {               
        public const string NetworkManagerInvited = "Network manager invited";
        public const string NewBuildNetworkRegistered = "New build network registered";
        public const string DesignatedDutyHolderInvited = "Designated Duty Holder invited";
        public const string ContributorInvited = "Contributor invited";
        
        public const string NetworkManagerJoined = "Network manager joined";
        public const string DesignatedDutyHolderJoined = "Designated Duty Holder joined";        
        public const string ContributorJoined = "Contributor joined";

        public const string DesignatedDutyHolderRejected = "Designated Duty Holder rejected";
        public const string ContributorRejected = "Contributor rejected";        
        public const string NetworkManagerRejected = "Network manager rejected";

        public const string AssessorAssignedToHN = "Assessor assigned to HN";
    }     
}
