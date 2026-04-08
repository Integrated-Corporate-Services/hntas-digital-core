using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    public class KpiConfigRequest
    {
        [JsonPropertyName("network_id")]
        public required string NetworkId { get; set; }

        [JsonPropertyName("elements")]
        public Dictionary<string, Dictionary<string, KpiRule>> Elements { get; set; } = [];
    }
}
