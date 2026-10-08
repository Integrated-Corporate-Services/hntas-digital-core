using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.V2
{
    [ExcludeFromCodeCoverage]
    public class KpiConfigRequestV2
    {
        [JsonPropertyName("network_id")]
        public required string NetworkId { get; set; }

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];

        // Option 1: Dedicated structural configuration block for the Carbon Calculator
        [JsonPropertyName("carbon_calculator")]
        public CarbonCalculatorConfigRequest? CarbonCalculator { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class CarbonCalculatorConfigRequest
    {
        // Maps EC-DATA keys (e.g., EC-DATA-53) to their schema validation/threshold parameters
        [JsonPropertyName("rules")]
        public Dictionary<string, KpiRule>? Rules { get; set; } = [];

        // Maps EC-DATA keys (e.g., EC-DATA-20) to their static default value settings
        [JsonPropertyName("defaults")]
        public Dictionary<string, ConfigDefault>? Defaults { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class ConfigDefault
    {
        [JsonPropertyName("value")]
        public required JsonElement Value { get; set; }

        // Safe helper to extract an integer value
        public int AsInt(int fallback = 0)
        {
            if (Value.ValueKind == JsonValueKind.Number && Value.TryGetInt32(out var result)) return result;
            if (Value.ValueKind == JsonValueKind.String && int.TryParse(Value.GetString(), out var parsedStr)) return parsedStr;
            return fallback;
        }

        // Safe helper to extract a string value
        public string AsString(string fallback = "")
        {
            if (Value.ValueKind == JsonValueKind.String) return Value.GetString() ?? fallback;

            // Prevent surrounding quotes from JSON string primitives when calling GetRawText()
            if (Value.ValueKind == JsonValueKind.Null) return fallback;

            var raw = Value.GetRawText();
            return raw.StartsWith('"') && raw.EndsWith('"') && raw.Length > 1
                ? raw[1..^1]
                : raw;
        }
    }
}
