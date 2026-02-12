using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace HNTAS.Core.Api.Data.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("oneloginId")]
        public string OneLoginId { get; set; }

        [BsonElement("orgId")]
        public string? OrgId { get; set; }

        [BsonElement("firstName")]
        public string? FirstName { get; set; }

        [BsonElement("lastName")]
        public string? LastName { get; set; }

        [BsonElement("jobTitle")]
        public string? JobTitle { get; set; }

        [BsonElement("emailId")]
        public string EmailId { get; set; }

        // This field was "preferred_contact_type" but is now "preferredContactType"
        [BsonElement("preferredContactType")]
        [BsonRepresentation(BsonType.String)]
        public PreferredContactType? PreferredContactType { get; set; }

        [BsonElement("landlineNumber")]
        public string? LandlineNumber { get; set; }

        [BsonElement("mobileNumber")]
        public string? MobileNumber { get; set; }

        [BsonElement("contactNumberExtension")]
        public string? ContactNumberExtension { get; set; }

        [BsonElement("roles")]
        [BsonRepresentation(BsonType.String)]
        public List<UserRole> Roles { get; set; } = [];

        [BsonElement("hnRoleMappings")]
        public List<HnRoleMapping> HnRoleMappings { get; set; } = [];

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public UserStatus Status { get; set; }

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

    }
}
