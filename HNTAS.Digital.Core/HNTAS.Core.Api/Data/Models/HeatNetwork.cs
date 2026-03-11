using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetwork
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("uHnId")]
        public string? UHnId { get; set; }

        [BsonElement("hnId")]
        public string? HnId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("address")]
        public RegisteredAddress? Address { get; set; }

        [BsonElement("ecDetails")]
        public ECDetails? ECDetails { get; set; }

        [BsonElement("pathway")]
        public string Pathway { get; set; }

        [BsonElement("registrationSource")]
        [BsonRepresentation(BsonType.String)]
        public RegistrationSource RegistrationSource { get; set; }

        [BsonElement("networkCharacteristics")]
        public NetworkCharacteristics? NetworkCharacteristics { get; set; }

        [BsonElement("networkElements")]
        public NetworkElements? NetworkElements { get; set; }

        [BsonElement("soa")]
        public Soa? Soa { get; set; }

        [BsonElement("meteringAndMonitoringStrategy")]
        public MeteringAndMonitoringStrategy? MeteringAndMonitoringStrategy { get; set; }

        [BsonElement("assessmentPlan")]
        public AssessmentPlan? AssessmentPlan { get; set; }

        [BsonElement("designConstructionLog")]
        public DesignConstructionLog? DesignConstructionLog { get; set; }

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; }

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("phase")]
        public string Phase { get; set; }
    }    
}
