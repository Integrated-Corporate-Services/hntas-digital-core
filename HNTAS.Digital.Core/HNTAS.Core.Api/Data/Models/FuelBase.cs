using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class FuelBase
    {
        [BsonElement("maximumHeatOutput")]
        public int? MaximumHeatOutput { get; set; }        
        [BsonElement("fuel")]
        public int? Fuel { get; set; }
    }
}
