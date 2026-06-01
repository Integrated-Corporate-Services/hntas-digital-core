using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.V2
{
    public class KpiConfigRequestV2
    {
        [JsonPropertyName("network_id")]
        public required string NetworkId { get; set; }

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];

        // Dedicated block for Carbon Calculator default threshold configs or settings
        // Maps ElementId -> (CarbonHntasCode -> DefaultValue/Rule)
        [JsonPropertyName("carbon_calculator_defaults")]
        public Dictionary<string, object>? CarbonCalculatorDefaults { get; set; } = new();
    }
}
