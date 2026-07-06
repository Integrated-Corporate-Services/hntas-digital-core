using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms.Dashboard
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkDashboardResponse
    {
        public List<HeatNetworkDashboardRow> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
