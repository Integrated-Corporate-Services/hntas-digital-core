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

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return "[redacted]";
            var parts = email.Split('@');
            var local = parts[0];
            var domain = parts[1];
            if (local.Length < 2)
                return $"* Hidden *@{domain}";
            return $"{local[0]}*****@{domain}";
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
                _logger.LogInformation("Email sent successfully to {MaskedEmail} for user {UserId}", MaskEmail(user.EmailId), user.Id);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for user {UserId}", user.EmailId, user.Id);
        }

                
        public async Task TrySendOrgUpdatedEmailAsync(string fullName, string userEmail, RegisteredAddress oldAddress, RegisteredAddress newAddress)
        {
            
            string formattedOldAddress = StringFormatter.FormatAddress(oldAddress);
            string formattedNewAddress = StringFormatter.FormatAddress(newAddress);

            var personalisation = new Dictionary<string, dynamic>
            {
                { "user_name", fullName },
                { "old_address", formattedOldAddress },
                { "new_address", formattedNewAddress }
            };

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                userEmail,
                _notificationSettings.OrgDetailsUpdatedEmailTemplateId,
                personalisation
            );

            if (emailSent)
                _logger.LogInformation("Organisation-updated email sent successfully to {EmailId}.", MaskEmail(userEmail));
            else
                _logger.LogWarning("Organisation-updated email failed to send to {EmailId}.", MaskEmail(userEmail));
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
                _logger.LogInformation("Email sent successfully to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
        }

        public async Task TrySendAssessorEmailAsync(string emailAddress, string hnName, string hnId, string contributorName)
        {

            var emailSent = await _govUkNotifyService.SendEmailAsync(
            emailAddress,
            _notificationSettings.AssessorNotificationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "stage_number",string.Empty},
                    { "stage_name",string.Empty },
                    { "hn_name",hnName },
                    { "hn_id", hnId },
                    { "contributor_name", contributorName },
                }
            );
        }

        public async Task TrySendAssessorAssessmentEmailAsync(string emailAddress, string hnName, string hnId, string assessmentResult)
        {

            var emailSent = await _govUkNotifyService.SendEmailAsync(
            emailAddress,
            _notificationSettings.AssessmentCompleteNotificationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "stage_number",string.Empty},
                    { "stage_name",string.Empty },
                    { "hn_name",hnName },
                    { "hn_id", hnId },
                    { "assessment_result", assessmentResult },
                }
            );
        }

        public async Task TrySendCertificationCompleteEmailAsync(string emailAddress, string hnName, string hnId)
        {

            var emailSent = await _govUkNotifyService.SendEmailAsync(
            emailAddress,
            _notificationSettings.CertificationCompleteNotificationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "hn_name",hnName },
                    { "hn_id", hnId },
                }
            );
        }
    }
}
