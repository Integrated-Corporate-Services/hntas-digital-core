using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms.PowerBi
{
    [ExcludeFromCodeCoverage]
    public class ArmsPowerBiUserReportResponse
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = null!;

        [JsonPropertyName("hn_id")]
        public string? HnId { get; set; }

        [JsonPropertyName("org_id")]
        public string? OrgId { get; set; }
    }
}
