using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    public class KpiConfigResponseV2
    {
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; } = null!;

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];

        [JsonPropertyName("carbon_calculator")]
        public CarbonCalculatorConfigResponse? CarbonCalculator { get; set; } = new();
    }

    public class CarbonCalculatorConfigResponse
    {
        [JsonPropertyName("rules")]
        public Dictionary<string, KpiRule>? Rules { get; set; } = new();

        [JsonPropertyName("defaults")]
        public Dictionary<string, JsonElement>? Defaults { get; set; } = new();
    }

}
