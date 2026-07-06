using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class NetworkTypeSelection
    {
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkType Type { get; set; }

        [BsonElement("otherNetworkDescription")]
        public string? OtherNetworkDescription { get; set; }
    }
}
