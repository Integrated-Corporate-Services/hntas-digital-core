using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class FuelBase
    {
        [BsonElement("id")]
        public string? Id { get; set; }
        [BsonElement("maximumHeatOutput")]
        public int? MaximumHeatOutput { get; set; }        
        [BsonElement("fuelId")]
        public int? FuelId { get; set; }
    }
}
