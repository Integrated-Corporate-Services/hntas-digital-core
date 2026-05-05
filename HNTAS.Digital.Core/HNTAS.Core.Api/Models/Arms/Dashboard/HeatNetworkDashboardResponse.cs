namespace HNTAS.Core.Api.Models.Arms.Dashboard
{
    public class HeatNetworkDashboardResponse
    {
        public List<HeatNetworkDashboardRow> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
