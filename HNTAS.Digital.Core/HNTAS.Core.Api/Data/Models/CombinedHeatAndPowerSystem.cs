using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class CombinedHeatAndPowerSystem : FuelBase
    {        
        [BsonElement("maximumElectricOutput")]
        public int? MaximumElectricOutput { get; set; }        
        [BsonElement("percentageOfElectricityExported")]
        public int? PercentageOfElectricityExported { get; set; }
    }
}
