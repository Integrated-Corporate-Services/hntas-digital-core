using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatAndFuelBase
    {
        [BsonElement("maximumHeatOutput")]
        public int? MaximumHeatOutput { get; set; }        
        [BsonElement("fuelOrHeatSource")]
        public int? FuelOrHeatSource { get; set; }
    }
}
