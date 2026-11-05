using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.NotificationHistory;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationHistoryController : ControllerBase
    {
        private readonly ILogger<NotificationHistoryController> _logger;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IUserService _userService;

        public NotificationHistoryController(ILogger<NotificationHistoryController> logger, INotificationHistoryService notificationHistoryService, IUserService userService)
        {
            _logger = logger;
            _notificationHistoryService = notificationHistoryService;
            _userService = userService;
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

                var notificationHistoryCount = await _notificationHistoryService.GetNotificationHistoryCount(request.UserId!, request.OrgId!);
                
                await _userService.UpdateNotificationHistoryCountAsync(request.UserId!, request.OrgId!, notificationHistoryCount);

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
                var user = await _userService.GetByIdAsync(userId);
                var orgId = string.Empty;
                if (user.Roles.Contains(UserRole.ResponsibleParty))
                {
                    orgId = user.OrgId!;
                }
                else
                {
                    orgId = user.ActiveContributingOrgId;
                }
                
                var notificationHistoryCount = await _notificationHistoryService.GetNotificationHistoryCount(userId, orgId!);
                var userStatsNotificatonCount = await _userService.GetNotificationHistoryCountAsync(userId, orgId!);
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
