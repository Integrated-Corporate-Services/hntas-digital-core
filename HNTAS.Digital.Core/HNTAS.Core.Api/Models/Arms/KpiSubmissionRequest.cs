using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiSubmissionRequest : BaseKpiSubmissionRequest
    {
        [JsonPropertyOrder(3)]
        [JsonPropertyName("elements")]
        public List<NetworkElementRequest> Elements { get; set; } = new();
    }
}
