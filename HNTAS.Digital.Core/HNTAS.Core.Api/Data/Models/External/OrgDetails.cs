using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models.External
{
    [ExcludeFromCodeCoverage]
    public class OrgDetails
    {
        [BsonElement("orgId")]
        public string OrgId { get; set; }

        [BsonElement("orgName")]
        public string OrgName { get; set; }

        [BsonElement("emailId")]
        public string EmailId { get; set; }

        [BsonElement("orgAddress")]
        public Address OrgAddress { get; set; }
    }
}
