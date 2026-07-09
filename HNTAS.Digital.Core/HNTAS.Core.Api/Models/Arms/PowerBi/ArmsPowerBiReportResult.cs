using HNTAS.Core.Api.Data.Models.Arms.Submission;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms.PowerBi
{
    [ExcludeFromCodeCoverage]
    public class ArmsPowerBiReportResult
    {
        public string OrgId { get; set; } = null!;
        public KpiSubmission KpiSubmission { get; set; } = null!;
    }
}
