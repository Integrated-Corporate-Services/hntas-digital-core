using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.NotificationHistory;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationHistoryController : ControllerBase
    {
        private readonly ILogger<NotificationHistoryController> _logger;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IUserService _userService;
        private readonly IUserStatsService _userStatsService;

        public NotificationHistoryController(ILogger<NotificationHistoryController> logger, INotificationHistoryService notificationHistoryService, IUserService userService, IUserStatsService userStatsService)
        {
            _logger = logger;
            _notificationHistoryService = notificationHistoryService;
            _userService = userService;
            _userStatsService = userStatsService;
        }
        [HttpGet("notification-history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NotificationHistoryResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NotificationHistory>> GetNotificationHistory(NotificationHistoryRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving Notification history for the user: {userId}", StringFormatter.Sanitize(request.UserId!));
                
                var result = await _notificationHistoryService.GetNotificationHistory(request);

                if (result is null)
                {
                    _logger.LogWarning("Notification history(s) are not found for the user: {userId}", StringFormatter.Sanitize(request.UserId!));
                    return NotFound();
                }

                var notificationHistoryCount = await _notificationHistoryService.GetNotificationHistoryCount(request.UserId!);
                await _userStatsService.UpdateNotificationHistoryCountAsync(request.UserId!, notificationHistoryCount);

                _logger.LogInformation("Notification history(s) are retrieved successfully for the user: {userId}", StringFormatter.Sanitize(request.UserId!));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Notification History for User ID: {userID}",
                StringFormatter.Sanitize(request.UserId!));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving Notification History.");
            }
        }

        [HttpGet("unread-notification-count")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<int>> UnreadNotificationCount(string userId, UserRole role)
        {
            try
            {
                var notificationHistoryCount = await _notificationHistoryService.GetNotificationHistoryCount(userId);
                var userStatsNotificatonCount = await _userStatsService.GetNotificationHistoryCountAsync(userId);
                var unreadNotificationCount = notificationHistoryCount - userStatsNotificatonCount;
                _logger.LogInformation("Unread Notification Count is retrieved successfully for the user: {userId}", StringFormatter.Sanitize(userId));
                return Ok(unreadNotificationCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Unread Notification Count for User ID: {userID}",
                StringFormatter.Sanitize(userId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving Unread Notification Count.");
            }
            
        }
    }
}
