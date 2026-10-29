using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class EnergyCentrePlantData
    {
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
