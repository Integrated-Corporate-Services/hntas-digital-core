using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class ManagedUserResponse
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = null!;

        [BsonElement("emailId")]
        public string EmailId { get; set; } = null!;

        [BsonElement("status")]
        public string Status { get; set; } = null!;

        [BsonElement("heatNetworks")]
        public List<HeatNetworkInfo>? HeatNetworks { get; set; }

        [BsonElement("roles")]
        public List<string>? Roles { get; set; }

        [BsonElement("invitedAt")]
        public DateTime? InvitedAt { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class HeatNetworkInfo
    {
        [BsonElement("hnId")]
        public string HnId { get; set; } = null!;

        [BsonElement("name")]
        public string Name { get; set; } = null!;
    }
}
