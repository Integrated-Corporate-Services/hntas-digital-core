using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Text.Json;
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
        public Dictionary<string, ConfigDefault>? CarbonCalculatorDefaults { get; set; } = new();
    }

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
            return Value.ValueKind == JsonValueKind.String ? Value.GetString() ?? fallback : Value.GetRawText();
        }
    }
}
