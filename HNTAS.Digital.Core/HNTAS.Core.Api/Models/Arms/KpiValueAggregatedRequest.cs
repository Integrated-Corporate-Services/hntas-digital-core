using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValueAggregatedRequest
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
