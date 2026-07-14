using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    public class KpiConfigResponse
    {
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; } = null!;

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];
    }
}
