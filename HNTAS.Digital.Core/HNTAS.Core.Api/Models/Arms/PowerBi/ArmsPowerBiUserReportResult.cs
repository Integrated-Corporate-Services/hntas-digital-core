using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms.PowerBi
{
    [ExcludeFromCodeCoverage]
    public class ArmsPowerBiUserReportResult
    {
        public string UserId { get; set; } = null!;
        public string? HnId { get; set; }
        public string? OrgId { get; set; }
    }
}
