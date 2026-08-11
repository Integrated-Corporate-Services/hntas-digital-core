using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class ElementGroup : ElementBase
    {
        [BsonElement("elementDisplayType")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkElementType ElementDisplayType { get; set; }
        [BsonElement("count")]
        [BsonRepresentation(BsonType.Int32)]
        public int? Count { get; set; }

        [BsonElement("soaStages")]
        public List<SoaStages>? SoaStages { get; set; } = [];
        [BsonElement("soaMilestones")]
        public List<SoaMilestone>? SoaMilestones { get; set; } = [];
    }
}
