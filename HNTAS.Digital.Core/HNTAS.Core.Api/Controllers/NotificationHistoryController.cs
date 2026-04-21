using HNTAS.Core.Api.Data.Models;
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
                _logger.LogInformation("Retrieving Notification history for the user: {userId}", request.UserId);
                var currentUser = await _userService.GetByIdAsync(request.UserId!);
                var currentUserRoles = currentUser.Roles.FirstOrDefault();

                var result = await _notificationHistoryService.GetNotificationHistory(request, currentUserRoles);

                if (result is null)
                {
                    _logger.LogWarning("Notification history(s) are not found for the user: {userId}", request.UserId);
                    return NotFound();
                }

                _logger.LogInformation("Notification history(s) are retrieved successfully for the user: {userId}", request.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Notification History for User ID: {userID}",
                StringFormatter.Sanitize(request.UserId!));
                throw;
            }
        }
    }
}
