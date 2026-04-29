namespace HNTAS.Core.Api.Interfaces
{
    public interface IUserStatsService
    {
        Task UpdateNotificationHistoryCountAsync(string userId, int notificationHistoryCount);
        Task<int> GetNotificationHistoryCountAsync(string userId);
    }
}
