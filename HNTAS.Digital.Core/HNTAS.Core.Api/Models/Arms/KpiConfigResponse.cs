using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    public class KpiConfigResponse
    {
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; } = null!;

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];
    }
}
