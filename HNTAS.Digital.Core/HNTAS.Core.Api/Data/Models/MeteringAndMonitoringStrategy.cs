using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class MeteringAndMonitoringStrategy : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus Status { get; set; }
        [BsonElement("documents")]
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }
}
