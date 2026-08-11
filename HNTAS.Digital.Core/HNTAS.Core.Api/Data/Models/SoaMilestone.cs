using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaMilestone
    {
        [BsonElement("milestoneId")]
        [BsonRepresentation(BsonType.String)]
        public Milestone? MilestoneId { get; set; }

        [BsonElement("soaStatuses")]
        public List<SoaStatusWithCountExistingNetwork>? SoaStatuses { get; set; } = [];

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
        public List<SoaAssessorExistingNetwork>? Assessors { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class SoaStatusWithCountExistingNetwork
    {
        [BsonElement("soaStatus")]
        [BsonRepresentation(BsonType.String)]
        public SoaStatus SoaStatus { get; set; }
        [BsonElement("count")]
        [BsonRepresentation(BsonType.Int32)]
        public int? Count { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SoaAssessorExistingNetwork
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
