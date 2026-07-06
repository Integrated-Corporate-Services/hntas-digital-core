using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class KpiSubmissionApiErrorResponse
    {
        public string Title { get; set; } = "Validation Failed";
        public int Status { get; set; }
        public string Detail { get; set; }
        public List<KpiSubmissionApiError> Errors { get; set; } = new();
    }
}
