using HNTAS.Core.Api.Models.NotificationHistory;
using HNTAS.Core.Api.Models.Soa;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.HeatNetwork
{
    [ExcludeFromCodeCoverage]
    public class ExistingNetworkResponse
    {
        public List<HeatNetworkResponse> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? UserId { get; set; }
    }
}
