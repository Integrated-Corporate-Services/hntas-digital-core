using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaMilestone
    {
        [BsonElement("milestoneId")]
        [BsonRepresentation(BsonType.String)]
        public Milestone? MilestoneId { get; set; }

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
}
