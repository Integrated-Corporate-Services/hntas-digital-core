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

        [BsonElement("networkElementInstanceName")]
        [BsonRepresentation(BsonType.String)]
        public string? NetworkElementInstanceName { get; set; }

        [BsonElement("soaMilestones")]
        public List<SoaMilestone>? SoaMilestones { get; set; }

        [BsonElement("elementName")]
        public string? ElementName { get; set; }
        [BsonElement("isMainEnergyCentre")]
        public bool? IsMainEnergyCentre { get; set; }
        [BsonElement("isSupplyNewBuildings")]
        public bool? IsSupplyNewBuildings { get; set; }
        [BsonElement("energyCentreStatus")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus? EnergyCentreStatus { get; set; }
        [BsonElement("combinedHeatAndPowerSystems")]
        public List<CombinedHeatAndPowerSystem>? CombinedHeatAndPowerSystems { get; set; }
        [BsonElement("heatPumpSystems")]
        public List<HeatPumpSystem>? HeatPumpSystems { get; set; }
        [BsonElement("heatRecoverySources")]
        public List<HeatRecoverySource>? HeatRecoverySources { get; set; }
        [BsonElement("boilerPlants")]
        public List<BoilerPlant>? BoilerPlants { get; set; }
    }    
}
