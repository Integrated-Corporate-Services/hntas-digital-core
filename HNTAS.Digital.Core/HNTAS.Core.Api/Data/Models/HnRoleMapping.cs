using HNTAS.Core.Api.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HnRoleMapping
    {
        [BsonElement("hnId")]
        public string HnId { get; set; } = null!;

        [BsonElement("role")]
        public ContributorRole Role { get; set; }
    }
}
