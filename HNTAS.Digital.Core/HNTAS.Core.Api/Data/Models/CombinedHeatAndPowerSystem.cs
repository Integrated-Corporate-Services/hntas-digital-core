using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class CombinedHeatAndPowerSystem : HeatAndFuelBase
    {        
        [BsonElement("maximumElectricOutput")]
        public int? MaximumElectricOutput { get; set; }        
        [BsonElement("operationalMode")]
        public string? OperationalMode { get; set; }
    }
}
