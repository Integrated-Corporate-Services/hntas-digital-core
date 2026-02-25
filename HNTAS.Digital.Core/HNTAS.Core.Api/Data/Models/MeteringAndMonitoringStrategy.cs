using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class MeteringAndMonitoringStrategy : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus Status { get; set; }
        [BsonElement("documents")]
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }
}
