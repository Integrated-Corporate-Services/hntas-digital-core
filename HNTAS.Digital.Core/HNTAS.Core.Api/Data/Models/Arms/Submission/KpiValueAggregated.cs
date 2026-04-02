using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValueAggregated
    {
        [JsonPropertyName("value")]
        [BsonElement("value")]
        public double Value { get; set; }
    }
}
