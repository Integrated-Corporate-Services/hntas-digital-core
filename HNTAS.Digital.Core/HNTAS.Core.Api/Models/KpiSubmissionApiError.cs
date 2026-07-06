using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class KpiSubmissionApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string? ElementId { get; set; }
        public List<string>? Kpis { get; set; }
    }
}
