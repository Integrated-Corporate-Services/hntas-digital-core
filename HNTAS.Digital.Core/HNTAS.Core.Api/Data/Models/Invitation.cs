using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Data.Models
{
    public class Invitation
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = null!;

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

        [Required(ErrorMessage = "Select a preferred contact number type.")]
        [BsonElement("preferredContactType")]
        [BsonRepresentation(BsonType.String)]
        public PreferredContactType PreferredContactType { get; set; } // Keep non-nullable 

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Landline number is not in a valid format.")]
        [MaxLength(20, ErrorMessage = "Landline number cannot exceed 20 characters.")]
        [BsonElement("landlineNumber")]
        public string? LandlineNumber { get; set; }

        [RegularExpression(@"^\d*$", ErrorMessage = "Extension must be numeric.")]
        [MaxLength(10, ErrorMessage = "Extension cannot exceed 10 characters.")]
        [BsonElement("contactNumberExtension")]
        public string? ContactNumberExtension { get; set; }

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Mobile number is not in a valid format.")]
        [MaxLength(13, ErrorMessage = "Mobile number cannot exceed 13 characters.")]
        [BsonElement("mobileNumber")]
        public string? MobileNumber { get; set; }

        [BsonElement("permissions")]
        public List<string>? Permissions { get; set; }

        [Required(ErrorMessage = "Invited Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Invited Email format.")]
        [BsonElement("invitedEmail")]
        public string InvitedEmail { get; set; } = null!;

        [Required(ErrorMessage = "Invited HN ID is required.")]
        [BsonElement("invitedHnId")]
        public string InvitedHnId { get; set; } = null!;

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
    }
}
