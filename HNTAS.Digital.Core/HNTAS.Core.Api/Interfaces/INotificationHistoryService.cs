using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface INotificationHistoryService
    {
        Task CreateAsync(NotificationHistory notificationHistory);
    }
}
