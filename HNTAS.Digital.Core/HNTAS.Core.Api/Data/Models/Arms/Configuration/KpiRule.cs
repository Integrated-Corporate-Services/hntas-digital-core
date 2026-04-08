using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Configuration
{
    public class KpiRule
    {
        [JsonPropertyName("lower_limit")]
        [BsonElement("lowerLimit")]
        public double LowerLimit { get; set; }

        [JsonPropertyName("upper_limit")]
        [BsonElement("upperLimit")]
        public double UpperLimit { get; set; }

        [JsonPropertyName("unit")]
        [BsonElement("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonPropertyName("is_mandatory")]
        [BsonElement("isMandatory")]
        public bool IsMandatory { get; set; }

        [JsonPropertyName("threshold_rule")]
        [BsonElement("thresholdRule")]
        public KpiThresholdRule ThresholdRule { get; set; } = new();
    }
}
