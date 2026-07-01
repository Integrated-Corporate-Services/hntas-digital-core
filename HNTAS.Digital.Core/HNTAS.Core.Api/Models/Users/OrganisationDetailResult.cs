using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class OrganisationDetailResult
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

        [BsonElement("heatNetworks")]
        public List<HeatNetworkUserResponse>? HeatNetworks { get; set; }
    }
}