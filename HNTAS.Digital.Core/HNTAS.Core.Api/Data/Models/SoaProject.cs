using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaProject
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("hnId")]
        [BsonRepresentation(BsonType.String)]
        public string HnId { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public SoaProjectStatus Status { get; set; }

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdBy")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatedBy { get; set; } = null!;

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("updatedBy")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UpdatedBy { get; set; }

        [BsonElement("journeyData")]
        public SoaJourneyData? JourneyData { get; set; }
    }

    public class SoaJourneyData
    {
        [BsonElement("networkType")]
        public NetworkTypeSelection? NetworkType { get; set; }

        [BsonElement("connectionTypes")]
        [BsonRepresentation(BsonType.String)]
        public List<ConnectionType>? ConnectionTypes { get; set; }

        [BsonElement("heatNetworkElements")]
        public List<HeatNetworkElement> HeatNetworkElements { get; set; } = [];

    }

    public class NetworkTypeSelection
    {
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkType Type { get; set; }

        [BsonElement("otherNetworkDescription")]
        public string? OtherNetworkDescription { get; set; }
    }

    public class HeatNetworkElement
    {
        [BsonElement("name")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementType Name { get; set; }

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("locations")]
        public List<string> Locations { get; set; } = new List<string>();

        [BsonElement("documents")]
        public List<UploadedDocument> Documents { get; set; } = new();
    }


    public class UploadedDocument
    {
        [BsonElement("fileName")]
        public string FileName { get; set; } = null!;

        [BsonElement("s3Key")]
        public string S3Key { get; set; } = null!;

        [BsonElement("phase")]
        [BsonRepresentation(BsonType.String)]
        public SoaPhase Phase { get; set; }

        [BsonElement("stage")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage Stage { get; set; }

        [BsonElement("uploadedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("uploadedBy")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UploadedBy { get; set; } = null!;
    }
}
