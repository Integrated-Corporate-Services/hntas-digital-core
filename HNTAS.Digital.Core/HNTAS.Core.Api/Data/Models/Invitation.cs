using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Data.Models
{
    public class Invitation
    {
        [Required(ErrorMessage = "Invitation Id is required.")]
        [BsonElement("id")]
        public string Id { get; set; } = null!;

        [Required(ErrorMessage = "Invitation First Name is required.")]
        [BsonElement("first_name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Invitation Last Name is required.")]
        [BsonElement("last_name")]
        public string LastName { get; set; } = null!;


        [Required(ErrorMessage = "Select a preferred contact number type.")]
        [BsonElement("preferred_contact_type")]
        [BsonRepresentation(BsonType.String)]
        public PreferredContactType PreferredContactType { get; set; } // Keep non-nullable if it's always required

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Landline number is not in a valid format.")]
        [MaxLength(20, ErrorMessage = "Landline number cannot exceed 20 characters.")]
        [BsonElement("landline_number")]
        public string? LandlineNumber { get; set; }

        [RegularExpression(@"^\d*$", ErrorMessage = "Extension must be numeric.")]
        [MaxLength(10, ErrorMessage = "Extension cannot exceed 10 characters.")]
        [BsonElement("contact_number_extension")]
        public string? ContactNumberExtension { get; set; }

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Mobile number is not in a valid format.")]
        [MaxLength(13, ErrorMessage = "Mobile number cannot exceed 13 characters.")]
        [BsonElement("mobile_number")]
        public string? MobileNumber { get; set; }

        [BsonElement("permissions")]
        public List<string>? Permissions { get; set; }

        [Required(ErrorMessage = "invited_roles is required.")]
        [BsonElement("invited_roles")]
        [BsonRepresentation(BsonType.String)] // Store enum names as strings in DB
        public List<ContributorRole> InvitedRoles { get; set; } = [];

        [Required(ErrorMessage = "invited_hn_id is required.")]
        [BsonElement("invited_hn_id")]
        public string InvitedHnId { get; set; } = null!;

        [Required(ErrorMessage = "Invited Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Invited Email format.")]
        [BsonElement("invited_email")]
        public string InvitedEmail { get; set; } = null!;

        [Required(ErrorMessage = "Invited At date/time is required.")]
        [BsonElement("invited_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime InvitedAt { get; set; }

        [BsonElement("invite_accepted_on")]
        public DateTime? AcceptedAt { get; set; }

        [BsonElement("invite_rejected_on")]
        public DateTime? RejectedAt { get; set; }

        [Required(ErrorMessage = "Invitation Status is required.")]
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]// Store enum name as string in DB
        public InvitationStatus Status { get; set; }
    }
}
