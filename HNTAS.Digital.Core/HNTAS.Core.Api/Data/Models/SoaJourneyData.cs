using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class SoaJourneyData
    {
        [BsonElement("networkType")]
        public NetworkTypeSelection? NetworkType { get; set; }

        [BsonElement("connectionTypes")]
        [BsonRepresentation(BsonType.String)]
        public List<ConnectionType>? ConnectionTypes { get; set; }

        [BsonElement("heatNetworkElements")]
        public List<HeatNetworkElement> HeatNetworkElements { get; set; } = [];

        [BsonElement("assessmentPlans")]
        public List<AssessmentPlanDocument> AssessmentPlans { get; set; } = [];
    }
}
