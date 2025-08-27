using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models.Users
{
    public class OrganisationResponse
    {
        [BsonElement("orgId")]
        public string OrgId { get; set; } = null!;
        [BsonElement("name")]
        public string Name { get; set; } = null!;
        [BsonElement("companiesHouseNumber")]
        public string? CompaniesHouseNumber { get; set; }

        [BsonElement("type")]
        public OrganisationType Type { get; set; }

        [BsonElement("registeredAddress")]
        public RegisteredAddress RegisteredAddress { get; set; }
    }
}