using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    public class KpiConfigResponseV2
    {
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; } = null!;

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];

        [JsonPropertyName("carbon_calculator")]
        public CarbonCalculatorConfigResponse? CarbonCalculator { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class CarbonCalculatorConfigResponse
    {
        [JsonPropertyName("rules")]
        public Dictionary<string, KpiRule>? Rules { get; set; } = new();

        [JsonPropertyName("defaults")]
        public Dictionary<string, JsonElement>? Defaults { get; set; } = new();
    }

}
