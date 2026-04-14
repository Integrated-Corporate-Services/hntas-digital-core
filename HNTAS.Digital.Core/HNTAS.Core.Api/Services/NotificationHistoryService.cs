using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class NotificationHistoryService : INotificationHistoryService
    {
        private readonly IMongoCollection<NotificationHistory> _notificationHistoryCollection;
        private readonly ILogger<NotificationHistoryService> _logger;

        public NotificationHistoryService(IOptions<AWSDocDbSettings> dbSettings,
            IMongoDatabase mongoDatabase,
            ILogger<NotificationHistoryService> logger)
        {
            _notificationHistoryCollection = mongoDatabase.GetCollection<NotificationHistory>(dbSettings.Value.NotificationHistoryCollectionName);
            _logger = logger;
            _logger.LogInformation("NotificationHistory initialized via Dependency Injection.");
        }

        public async Task CreateAsync(NotificationHistory notificationHistory)
        {
            await _notificationHistoryCollection.InsertOneAsync(notificationHistory);            

            _logger.LogInformation("Notification history inserted...");
        }
    }
}
