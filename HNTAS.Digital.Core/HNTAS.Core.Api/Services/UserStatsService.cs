using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class UserStatsService : IUserStatsService
    {
        private readonly IMongoCollection<UserStats> _userStatsCollection;
        private readonly ILogger<UserStatsService> _logger;

        public UserStatsService(IOptions<AWSDocDbSettings> dbSettings, IMongoDatabase mongoDatabase, ILogger<UserStatsService> logger)
        {
            _userStatsCollection = mongoDatabase.GetCollection<UserStats>(dbSettings.Value.UserStatsCollectionName);
            _logger = logger;
            _logger.LogInformation("UserStatsService initialized via Dependency Injection.");
        }

        public async Task UpdateNotificationHistoryCountAsync(string userId, int notificationHistoryCount)
        {
            var filter = Builders<UserStats>.Filter.Eq(us => us.UserId, userId);
            var update = Builders<UserStats>.Update.Set(us => us.NotificationHistoryCount, notificationHistoryCount);
            var options = new UpdateOptions { IsUpsert = true };
            await _userStatsCollection.UpdateOneAsync(filter, update, options);
            _logger.LogInformation("Added/Updated UserStats for user ID: {UserId} with NotificationHistoryCount: {NotificationHistoryCount}",
                StringFormatter.Sanitize(userId), notificationHistoryCount);
        }

        public async Task<int> GetNotificationHistoryCountAsync(string userId)
        {
            var filter = Builders<UserStats>.Filter.Eq(us => us.UserId, userId);
            var userStats = await _userStatsCollection.Find(filter).FirstOrDefaultAsync();
            return userStats?.NotificationHistoryCount ?? 0;
        }
    }
}
