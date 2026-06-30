using HNTAS.Core.Api.Data.Models.Arms.Submission;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    public class BaseKpiSubmissionRequest
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("meta_data")]
        public required KpiMetadata MetaData { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("consumer_connection_aggregated_kpis")]
        public Dictionary<string, KpiValueAggregatedRequest>? ConsumerConnectionAggregatedKpis { get; set; }
    }
}
