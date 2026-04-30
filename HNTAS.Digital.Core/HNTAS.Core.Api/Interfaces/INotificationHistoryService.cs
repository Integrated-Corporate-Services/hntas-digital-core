using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NotificationHistory;

namespace HNTAS.Core.Api.Interfaces
{
    public interface INotificationHistoryService
    {
        Task CreateAsync(NotificationHistory notificationHistory);
        Task<NotificationHistoryResponse> GetNotificationHistory(NotificationHistoryRequest notificatoinHistoryRequest, UserRole role);
        Task<int> GetNotificationHistoryCount(string userId, UserRole role);
    }
}
