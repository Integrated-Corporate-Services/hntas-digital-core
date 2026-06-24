using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class NetworkElementRequest
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("elementId")]
        public required string ElementId { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("kpis")]
        public Dictionary<string, KpiValueRequest> Kpis { get; set; } = new();
    }
}
