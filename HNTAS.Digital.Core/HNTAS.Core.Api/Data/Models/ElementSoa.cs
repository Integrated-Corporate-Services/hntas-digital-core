using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaStages
    {
        [BsonElement("stageId")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage? StageId { get; set; }

        [BsonElement("soaStatus")]
        public string? SoaStatus { get; set; }
        [BsonElement("soaStatusUpdatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? SoaStatusUpdatedAt { get; set; }
        [BsonElement("soaStatusUpdatedBy")]
        public string? SoaStatusUpdatedBy { get; set; }
        [BsonElement("assessorUpdatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? AssessorUpdatedAt { get; set; }
        [BsonElement("assessorUpdatedBy")]
        public string? AssessorUpdatedBy { get; set; }
        [BsonElement("assessor")]
        public SoaAssessor? Assessor { get; set; }

    }

    public class SoaAssessor
    {
        [BsonElement("firstName")]
        public string FirstName { get; set; } = null!;

        [BsonElement("lastName")]
        public string LastName { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;
        
        [BsonElement("assessment")]
        public string Assessment { get; set; } = null!;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public UserStatus Status { get; set; }
    }
}
