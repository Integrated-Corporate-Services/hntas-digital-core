using HNTAS.Core.Api.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.NotificationHistory
{
    [ExcludeFromCodeCoverage]
    public class NotificationHistoryResponse
    {
        public List<NotificationHistoryData> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? UserId { get; set; }

    }

    public class NotificationHistoryData
    {
        public string? Id { get; set; }
        public NotificationHistoryType NotificationType { get; set; }
        public List<string> ActorsId { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Action { get; set; }
        public string? ActionLink { get; set; }
        public List<string> EligibleRoles { get; set; } = [];
        public string? HeatNetworkId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
