using HNTAS.Core.Api.Data.Models;
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
        public string Type { get; set; }

        [BsonElement("registeredAddress")]
        public RegisteredAddress RegisteredAddress { get; set; }
    }
}