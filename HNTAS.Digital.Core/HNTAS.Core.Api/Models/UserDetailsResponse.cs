using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Models.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models
{
    public class UserDetailsResponse
    {
        public string Id { get; set; }

        [BsonElement("oneloginId")]
        public string OneLoginId { get; set; }

        [BsonElement("firstName")]
        public string? FirstName { get; set; }

        [BsonElement("lastName")]
        public string? LastName { get; set; }

        public string? FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                    return null;

                var formattedFirst = StringFormatter.ToTitleCaseSingleWord(FirstName ?? "");
                var formattedLast = StringFormatter.ToTitleCaseSingleWord(LastName ?? "");

                return $"{formattedFirst} {formattedLast}".Trim();
            }
        }

        [BsonElement("emailId")]
        public string EmailId { get; set; }

        [BsonElement("jobTitle")]
        public string? JobTitle { get; set; }

        [BsonElement("preferredContactType")]
        public PreferredContactType? PreferredContactType { get; set; }

        [BsonElement("landlineNumber")]
        public string? LandlineNumber { get; set; }

        [BsonElement("mobileNumber")]
        public string? MobileNumber { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("roles")]
        public List<UserRole>? Roles { get; set; }

        [BsonElement("organisation")]
        public OrganisationResponse? Organisation { get; set; }

        [BsonElement("neatNetworks")]
        public List<HeatNetworkResponse>? HeatNetworks { get; set; }

    }
}
