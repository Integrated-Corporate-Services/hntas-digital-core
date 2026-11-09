using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatRecoverySource
    {
        [BsonElement("maximumHeatOutput")]
        public int? MaximumHeatOutput { get; set; }
        [BsonElement("fuelOrHeatSource")]
        public string? FuelOrHeatSource { get; set; }
    }
}
