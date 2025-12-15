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

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public OrganisationType Type { get; set; }

        [BsonElement("companiesHouseNumber")]
        public string? CompaniesHouseNumber { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("registeredAddress")]
        public RegisteredAddress RegisteredAddress { get; set; }

        [BsonElement("hnIds")]
        public List<string> HnIds { get; set; } = [];

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; }

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("lastModifiedBy")]
        public string? LastModifiedBy { get; set; }

        [BsonElement("lastModifiedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? LastModifiedAt { get; set; }

        [BsonElement("rpUserId")]
        public string? RpUserId { get; set; }
    }

}
