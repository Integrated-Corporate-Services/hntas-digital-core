using System.Text.Json;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.V2
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiSubmissionRequestV2 : BaseKpiSubmissionRequest
    {
        /// <summary>
        /// Dedicated block for Carbon Calculator fields on EnergyCentre types.
        /// Organised into sectioned blocks (e.g., "metadata", "energy_totals") where 
        /// each KPI key contains a nested metadata object wrapper (value, is_kpi_imputed, kpi_imputation_details).
        /// </summary>
        [JsonPropertyOrder(3)]
        [JsonPropertyName("carbon_calculator_inputs")]
        public Dictionary<string, Dictionary<string, CCKpiValueRequest>>? CarbonInputsV2 { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class CCKpiValueRequest
    {
        [JsonPropertyName("value")]
        public required JsonElement Value { get; set; }

        [JsonPropertyName("is_imputed")]
        public bool IsImputed { get; set; } = false;

        [JsonPropertyName("imputation_details")]
        public string? ImputationDetails { get; set; }

        public int AsInt(int fallback = 0)
        {
            if (Value.ValueKind == JsonValueKind.Number && Value.TryGetInt32(out var result)) return result;
            if (Value.ValueKind == JsonValueKind.String && int.TryParse(Value.GetString(), out var parsedStr)) return parsedStr;
            return fallback;
        }

        public double AsDouble(double fallback = 0.0)
        {
            // 1. If it's already a native JSON number type, try to extract it as a double
            if (Value.ValueKind == JsonValueKind.Number && Value.TryGetDouble(out var result))
                return result;

            // 2. If it was stored as a text string, attempt a safe parsing conversion
            if (Value.ValueKind == JsonValueKind.String && double.TryParse(Value.GetString(), out var parsedStr))
                return parsedStr;

            // 3. Return default fallback if the structure is empty, null, or a non-numeric type
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
