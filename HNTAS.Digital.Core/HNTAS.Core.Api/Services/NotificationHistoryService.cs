using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.NotificationHistory;
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

        public async Task<NotificationHistoryResponse> GetNotificationHistory(NotificationHistoryRequest notificatoinHistoryRequest)
        {
            try
            {
                var filter = Builders<NotificationHistory>.Filter.AnyEq(nh => nh.ActorsId, notificatoinHistoryRequest.UserId);
                
                var totalCount = await _notificationHistoryCollection.CountDocumentsAsync(filter);

                var sortDirection = notificatoinHistoryRequest.SortDirection?.ToLowerInvariant() ?? "desc";
                var sort = sortDirection == "desc"
                    ? Builders<NotificationHistory>.Sort.Descending(notificatoinHistoryRequest.SortBy ?? "timestamp")
                    : Builders<NotificationHistory>.Sort.Ascending(notificatoinHistoryRequest.SortBy ?? "timestamp");

                var notificationHistories = await _notificationHistoryCollection
                    .Find(filter)
                    .Sort(sort)
                    .Skip((notificatoinHistoryRequest.Page - 1) * notificatoinHistoryRequest.PageSize)
                    .Limit(notificatoinHistoryRequest.PageSize)
                    .ToListAsync();

                var notificationHistoryData = notificationHistories.Select(nh => new NotificationHistoryData
                {
                    Id = nh.Id,
                    NotificationType = nh.NotificationType,
                    ActorsId = nh.ActorsId,
                    Subject = nh.Subject,
                    Description = nh.Description,
                    Timestamp = nh.Timestamp,
                    Action = nh.Action,
                    EligibleRoles = nh.EligibleRoles,
                    HeatNetworkId = nh.HeatNetworkId,
                    CreatedBy = nh.CreatedBy,
                }).ToList();

                var notificationHistoryResponses =
                    new NotificationHistoryResponse
                    {
                        Items = notificationHistoryData,
                        PageNumber = notificatoinHistoryRequest.Page,
                        PageSize = notificatoinHistoryRequest.PageSize,
                        TotalCount = (int)totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)notificatoinHistoryRequest.PageSize),
                        UserId = notificatoinHistoryRequest.UserId
                    };

                _logger.LogInformation("Retrieved notification history records for user ID: {UserId}",
                    StringFormatter.Sanitize(notificatoinHistoryRequest.UserId!));

                return notificationHistoryResponses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Notification History for User ID: {userID}", StringFormatter.Sanitize(notificatoinHistoryRequest.UserId!));
                throw;
            }

        }

        public async Task<int> GetNotificationHistoryCount(string userId)
        {
            var filter = Builders<NotificationHistory>.Filter.AnyEq(nh => nh.ActorsId, userId);            
            var count = await _notificationHistoryCollection.CountDocumentsAsync(filter);
            _logger.LogInformation("Retrieved notification history count for user ID: {UserId}, Count: {Count}",
                StringFormatter.Sanitize(userId), count);
            return (int)count;
        }
    }
}
