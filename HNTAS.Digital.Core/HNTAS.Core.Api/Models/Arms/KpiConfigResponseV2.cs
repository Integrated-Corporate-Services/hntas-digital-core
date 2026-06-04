using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Models.Arms.V2;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    public class KpiConfigResponseV2
    {
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; } = null!;

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];

        [JsonPropertyName("carbon_calculator_defaults")]
        public Dictionary<string, ConfigDefault>? CarbonCalculatorDefaults { get; set; } = new();
    }
}
