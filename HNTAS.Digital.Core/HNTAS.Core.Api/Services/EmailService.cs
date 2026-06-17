using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

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


        public async Task TrySendOrgUpdatedEmailAsync(string fullName, string userEmail, string oldNameAndAddress, string newNameAndAddress)
        {

            var personalisation = new Dictionary<string, dynamic>
            {
                { "user_name", fullName },
                { "old_address", oldNameAndAddress },
                { "new_address", newNameAndAddress }
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

        public async Task TrySendHeatNetworkRegistrationEmailAsync(string userEmail, string fullName, string hnId, string hnName)
        {
            var personalisation = new Dictionary<string, dynamic>
            {
                { "full_name", fullName },
                { "hn_name", hnName },
                { "hn_id", hnId },
                { "digital_service_link", "https://signin.integration.account.gov.uk/sign-in-or-create" }
            };

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                userEmail,
                _notificationSettings.HeatNetworkRegistrationEmailTemplateId,
                personalisation
            );

            if (emailSent)
                _logger.LogInformation("Heat network registered email sent successfully to {EmailId}.", MaskEmail(userEmail));
            else
                _logger.LogWarning("Heat network registered email failed to send to {EmailId}.", MaskEmail(userEmail));
        }

        // --- Private Helper Method ---
        public async Task TrySendHeatNetworkInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName)
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
                { "subject_name", heatNetworkName ?? string.Empty },
                { "hntas-digital-service-link", fullUrl },
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
        }

        public async Task TrySendOrganisationInvitationEmailAsync(Invitation invitation, string token, string organisationName, string inviterName)
        {
            if (invitation == null || string.IsNullOrWhiteSpace(invitation.InvitedEmail))
            {
                _logger.LogInformation("Skipping email: missing Invitation or InvitedEmail for invitation {InvitationId}", invitation?.Id);
                return;
            }

            var fullUrl = $"{_hntasServiceSettings.BaseUrl.TrimEnd('/')}{_hntasServiceSettings.InvitationPath}?token={token}";

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                invitation.InvitedEmail,
                _notificationSettings.OrganisationUserInvitationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "org_name", organisationName ?? string.Empty },
                    { "rp_name" , inviterName },
                    { "link-to-register", fullUrl },
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

        public async Task TrySendHNDiscontinedEmailAsync(User userToUpdate, string hnName, ContributorRole contributorRole)
        {
            var emailSent = await _govUkNotifyService.SendEmailAsync(
           userToUpdate.EmailId,
           _notificationSettings.ContributorHeatNetworkDiscontinuedTemplatedId,
               new Dictionary<string, dynamic>
               {
                    { "hn_user_name", $"{StringFormatter.ToTitleCaseSingleWord(userToUpdate.FirstName)} {StringFormatter.ToTitleCaseSingleWord(userToUpdate.LastName)}" },
                    { "hn_name",hnName },
                    { "hn_role", contributorRole.GetDescription() },
               }
           );
        }

        public async Task TrySendOfgemDataForExistingOrgOrRpEmailAsync(OfgemDataModelForNotification ofgemData)
        {
            var hnIds = ofgemData.HeatNetworkIds;            
            string formatedHnIds = hnIds != null && hnIds.Count > 1
                ? string.Join(Environment.NewLine, hnIds.Select(i => $"* {i}"))
                : (hnIds != null && hnIds.Count == 1 ? hnIds[0] : "N/A");

            var startUrl = _hntasServiceSettings.BaseUrl;
            var emailSent = await _govUkNotifyService.SendEmailAsync(
           ofgemData.UserEmailId,
           _notificationSettings.OfgemDataForExistingOrgOrRpTemplateId,
               new Dictionary<string, dynamic>
               {
                    { "hntas-org-name", ofgemData.OrganisationName},
                    { "hn-ids", formatedHnIds },
                    { "hntas-digital-link", startUrl }
               }
           );
        }

        public async Task TrySendOfgemDataForNewRpEmailAsync(OfgemDataModelForNotification ofgemData)
        {
            var hnIds = ofgemData.HeatNetworkIds;
            string formatedHnIds = hnIds != null && hnIds.Count > 1
                ? string.Join(Environment.NewLine, hnIds.Select(i => $"* {i}"))
                : (hnIds != null && hnIds.Count == 1 ? hnIds[0] : "N/A");

            var startUrl = _hntasServiceSettings.BaseUrl;
            var emailSent = await _govUkNotifyService.SendEmailAsync(
           ofgemData.UserEmailId,
           _notificationSettings.OfgemDataForNewRpTemplateId,
               new Dictionary<string, dynamic>
               {
                    { "ofgem-org-name", ofgemData.OrganisationName},
                    { "hn-ids", formatedHnIds },
                    { "hntas-digital-link", startUrl }
               }
           );
        }
    }
}
