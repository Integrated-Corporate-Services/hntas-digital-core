using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms
{
    [ExcludeFromCodeCoverage]
    public class KpiWarningResponse
    {
        public string Code { get; set; } = null!;
        public string ElementId { get; set; } = null!;
        public string Kpi { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
