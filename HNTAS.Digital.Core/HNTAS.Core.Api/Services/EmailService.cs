using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace HNTAS.Core.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IGovUkNotifyService _govUkNotifyService;
        private readonly NotificationSettings _notificationSettings;
        private readonly HntasServiceSettings _hntasServiceSettings;

        public EmailService(
            ILogger<EmailService> logger,
            IGovUkNotifyService govUkNotifyService,
            IOptions<NotificationSettings> options,
            IOptions<HntasServiceSettings> hntasServiceOptions)
        {
            _logger = logger;
            _govUkNotifyService = govUkNotifyService;
            _notificationSettings = options?.Value;
            _hntasServiceSettings = hntasServiceOptions?.Value;
        }

        public async Task TrySendOrgCreatedEmailAsync(User user, Organisation organization)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.EmailId) || string.IsNullOrWhiteSpace(user.OrgId) || organization == null)
            {
                _logger.LogInformation("Skipping email: missing User, Organization, EmailId, or OrgId for user {UserId}", user?.Id);
                return;
            }

            string orgName = organization.Name;
            string firstName = StringFormatter.ToTitleCaseSingleWord(user.FirstName ?? "");
            string lastName = StringFormatter.ToTitleCaseSingleWord(user.LastName ?? "");
            string fullName = $"{firstName} {lastName}".Trim();
            string formattedAddress = StringFormatter.FormatAddress(organization.RegisteredAddress);

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                user.EmailId,
                _notificationSettings.OrgCreatedEmailTemplateId,
                new Dictionary<string, dynamic>
                {
                { "orgName", orgName },
                { "orgId", user.OrgId },
                { "fullName", fullName },
                { "address", formattedAddress }
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for user {UserId}", user.EmailId, user.Id);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for user {UserId}", user.EmailId, user.Id);
        }


        // --- Private Helper Method ---
        public async Task TrySendInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName)
        {
            if (invitation == null || string.IsNullOrWhiteSpace(invitation.InvitedEmail))
            {
                _logger.LogInformation("Skipping email: missing Invitation or InvitedEmail for invitation {InvitationId}", invitation?.Id);
                return;
            }

            var fullUrl = $"{_hntasServiceSettings.BaseUrl.TrimEnd('/')}{_hntasServiceSettings.InvitationPath}?token={token}";

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                invitation.InvitedEmail,
                _notificationSettings.ContributorInvitationTemplatedId,
                new Dictionary<string, dynamic>
                {
                { "subject_name", heatNetworkName },
                { "hntas-digital-service-link", fullUrl },
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for InviterUserId {UserId}", invitation.InvitedEmail, invitation.InviterUserId);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for InviterUserId {UserId}", invitation.InvitedEmail, invitation.InviterUserId);
        }
    }
}
