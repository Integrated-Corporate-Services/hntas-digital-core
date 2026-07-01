using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class Invitation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Inviter User Id is required.")]
        [BsonElement("inviterUserId")] // Added to link to the user who sent the invite
        [BsonRepresentation(BsonType.ObjectId)] // To match the _id of the User document
        public string InviterUserId { get; set; } = null!;

        [Required(ErrorMessage = "First Name is required.")]
        [BsonElement("firstName")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        [BsonElement("lastName")]
        public string LastName { get; set; } = null!;

        [BsonElement("permissions")]
        public List<string>? Permissions { get; set; }

        [Required(ErrorMessage = "Invited Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Invited Email format.")]
        [BsonElement("invitedEmail")]
        public string InvitedEmail { get; set; } = null!;

        [BsonElement("invitedHnId")]
        public string? InvitedHnId { get; set; }

        [BsonElement("invitedOrgId")]
        public string? InvitedOrgId { get; set; }

        [Required(ErrorMessage = "Invited roles are required.")]
        [BsonElement("invitedRoles")]
        [BsonRepresentation(BsonType.String)] // Store enum names as strings in DB
        public List<ContributorRole> InvitedRoles { get; set; } = [];

        [Required(ErrorMessage = "Invited At date/time is required.")]
        [BsonElement("invitedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime InvitedAt { get; set; }

        [BsonElement("inviteAcceptedOn")]
        public DateTime? AcceptedAt { get; set; }

        [BsonElement("inviteRejectedOn")]
        public DateTime? RejectedAt { get; set; }

        [Required(ErrorMessage = "Invitation Status is required.")]
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)] // Store enum name as string in DB
        public InvitationStatus Status { get; set; }

        [BsonElement("replacedUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ReplacedUserId { get; set; }

        [BsonElement("rolesToReplace")]

        [BsonRepresentation(BsonType.String)]
        public List<ContributorRole>? RolesToReplace { get; set; }
    }
}
