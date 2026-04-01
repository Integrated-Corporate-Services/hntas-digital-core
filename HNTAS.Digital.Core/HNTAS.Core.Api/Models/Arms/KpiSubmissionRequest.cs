using HNTAS.Core.Api.Data.Models.Arms.Submission;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiSubmissionRequest
    {
        [JsonPropertyName("meta_data")]
        public required KpiMetadata MetaData { get; set; }

        [JsonPropertyName("consumer_connection_aggregated_kpis")]
        public Dictionary<string, KpiValueAggregated>? ConsumerConnectionAggregatedKpis { get; set; }

        [JsonPropertyName("elements")]
        public List<NetworkElement> Elements { get; set; } = new();
    }
}
