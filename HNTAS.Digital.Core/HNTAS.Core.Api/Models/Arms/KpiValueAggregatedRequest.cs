using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiValueAggregatedRequest
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
}
