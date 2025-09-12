using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Organisation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("orgId")]
        public string? OrgId { get; set; }

        [BsonElement("type")] // Changed "OrganisationType" to "type"
        [BsonRepresentation(BsonType.String)]
        public OrganisationType Type { get; set; }

        [BsonElement("companiesHouseNumber")] // Changed from "companies_house_number"
        public string? CompaniesHouseNumber { get; set; }

        [BsonElement("name")] // Changed from "name"
        public string Name { get; set; }

        [BsonElement("registeredAddress")] // Changed from "registered_address"
        public RegisteredAddress RegisteredAddress { get; set; }
    }

}
