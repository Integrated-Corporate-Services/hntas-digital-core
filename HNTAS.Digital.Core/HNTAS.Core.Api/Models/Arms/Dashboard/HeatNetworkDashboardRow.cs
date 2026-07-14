using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms.Dashboard
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkDashboardRow
    {
        public string HnId { get; set; } = string.Empty;
        public string NetworkName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string DataPeriod { get; set; } = string.Empty;
        public string? SubmissionId { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
