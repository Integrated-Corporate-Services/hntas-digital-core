using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class NetworkElementRequest
    {
        [JsonPropertyName("elementId")]
        public required string ElementId { get; set; }

        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("kpis")]
        public Dictionary<string, KpiValueRequest> Kpis { get; set; } = new();
    }
}
