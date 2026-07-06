using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class SoaStages
    {
        [BsonElement("stageId")]
        [BsonRepresentation(BsonType.String)]
        public SoaStage? StageId { get; set; }

        [BsonElement("soaStatuses")]
        public List<SoaStatusWithCount>? SoaStatuses { get; set; } = [];
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
        public List<SoaAssessor>? Assessors { get; set; } = [];

    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    public class SoaStatusWithCount
    {
        [BsonElement("soaStatus")]
        [BsonRepresentation(BsonType.String)]
        public SoaStatus SoaStatus { get; set; }
        [BsonElement("count")]
        [BsonRepresentation(BsonType.Int32)]
        public int? Count { get; set; }
    }
}
