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

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; }

        [BsonElement("createdDate")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonElement("lastModifiedBy")]
        public string? LastModifiedBy { get; set; }

        [BsonElement("lastModifiedDate")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? LastModifiedDate { get; set; }

        [BsonElement("rpUserId")]
        public string? RpUserId { get; set; }
    }

}
