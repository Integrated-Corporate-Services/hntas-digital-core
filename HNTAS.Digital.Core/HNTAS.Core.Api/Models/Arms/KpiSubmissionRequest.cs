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
        public Dictionary<string, KpiValueAggregatedRequest>? ConsumerConnectionAggregatedKpis { get; set; }

        [JsonPropertyName("elements")]
        public List<NetworkElementRequest> Elements { get; set; } = new();
    }
}
