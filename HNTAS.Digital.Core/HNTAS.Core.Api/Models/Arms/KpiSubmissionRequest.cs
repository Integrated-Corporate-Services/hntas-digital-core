using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class KpiSubmissionRequest : BaseKpiSubmissionRequest
    {
    }
}
