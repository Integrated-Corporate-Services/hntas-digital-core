using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class Element : ElementBase
    {        
        [BsonElement("elementId")]
        public string? ElementId { get; set; }        

        [BsonElement("soaMilestones")]
        public List<SoaMilestone>? SoaMilestones { get; set; }

        [BsonElement("elementName")]
        public string? ElementName { get; set; }
        [BsonElement("isMainEnergyCentre")]
        public bool? IsMainEnergyCentre { get; set; }
        [BsonElement("isSupplyNewBuildings")]
        public bool? IsSupplyNewBuildings { get; set; }
        [BsonElement("energyCentrePlantData")]
        public EnergyCentrePlantData? EnergyCentrePlantData { get; set; }
    }    
}
