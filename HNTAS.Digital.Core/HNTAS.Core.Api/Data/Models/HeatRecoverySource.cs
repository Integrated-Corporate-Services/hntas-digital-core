using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatRecoverySource
    {
        [BsonElement("id")]
        public string? Id { get; set; }
        [BsonElement("maximumHeatOutput")]
        public int? MaximumHeatOutput { get; set; }
        [BsonElement("heatSource")]
        public string? HeatSource { get; set; }
    }
}
