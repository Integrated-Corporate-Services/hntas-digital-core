using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
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
        private readonly string _webBaseUrl;

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
            _webBaseUrl = Environment.GetEnvironmentVariable("WEB_BASE_URL") ?? throw new InvalidOperationException("Web API URL is not configured. Set WEB_BASE_URL environment variable ");
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
                    { "address", formattedAddress },
                    { "digital-link" , _webBaseUrl.TrimEnd('/') + _hntasServiceSettings.HomePath }
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
                _logger.LogInformation("Organisation-updated email sent successfully to {User}.", fullName);
            else
                _logger.LogWarning("Organisation-updated email failed to send to {User}", fullName);
        }

        public async Task TrySendHeatNetworkRegistrationEmailAsync(string userEmail, string fullName, string hnId, string hnName, string orgName)
        {
            var personalisation = new Dictionary<string, dynamic>
            {
                { "full_name", fullName },
                { "hn_name", hnName },
                { "hn_id", hnId },
                { "org_name", orgName },
                { "digital-link" , _webBaseUrl.TrimEnd('/') + _hntasServiceSettings.HomePath }
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
        public async Task TrySendHeatNetworkInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName, string organisationName)
        {
            if (invitation == null || string.IsNullOrWhiteSpace(invitation.InvitedEmail))
            {
                _logger.LogInformation("Skipping email: missing Invitation or InvitedEmail for invitation {InvitationId}", invitation?.Id);
                return;
            }

            var fullUrl = $"{_webBaseUrl.TrimEnd('/')}{_hntasServiceSettings.InvitationPath}?token={token}";

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                invitation.InvitedEmail,
                _notificationSettings.ContributorInvitationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "full_name", StringFormatter.ToTitleCaseSingleWord(invitation.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(invitation.LastName) },
                    { "hn_name", heatNetworkName ?? string.Empty },
                    { "hn_id", invitation.InvitedHnId ?? string.Empty },
                    { "org_name", organisationName ?? string.Empty },
                    { "is_ddh", invitation.InvitedRoles.Contains(ContributorRole.DesignatedDutyHolder)},
                    { "digital-link", fullUrl },
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

            var fullUrl = $"{_webBaseUrl.TrimEnd('/')}{_hntasServiceSettings.InvitationPath}?token={token}";

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                invitation.InvitedEmail,
                _notificationSettings.OrganisationUserInvitationTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "org_name", organisationName ?? string.Empty },
                    { "full_name", StringFormatter.ToTitleCaseSingleWord(invitation.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(invitation.LastName) },
                    { "digital-link", fullUrl },
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

            var startUrl = _webBaseUrl;
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

            var startUrl = _webBaseUrl;
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

        public async Task TrySendRejectionInvitationEmailAsync(Invitation invitation, string inviter_full_name, string inviterEmail, string invited_org_name)
        {
            if (invitation == null || string.IsNullOrWhiteSpace(invitation.InvitedEmail))
            {
                _logger.LogWarning("Skipping email: missing Invitation or InvitedEmail for invitation {InvitationId}", invitation?.Id);
                return;
            }

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                inviterEmail,
                _notificationSettings.InvitationRejectionTemplatedId,
                new Dictionary<string, dynamic>
                {
                    { "full_name", inviter_full_name },
                    { "org_name", invited_org_name ?? string.Empty },
                    { "invitee_full_name", StringFormatter.ToTitleCaseSingleWord(invitation.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(invitation.LastName) },
                    { "invited_role", invitation.InvitedRoles.FirstOrDefault().GetDescription() ?? string.Empty },
                    { "digital-link" , _webBaseUrl.TrimEnd('/') + _hntasServiceSettings.HomePath }
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
        }

        public async Task TrySendNetworkManagerAcceptedToRPEmailAsync(Invitation invitation, string inviter_full_name, string inviter_emailId, string orgName)
        {
            if (invitation == null || string.IsNullOrWhiteSpace(invitation.InvitedEmail))
            {
                _logger.LogWarning("Skipping email: missing Invitation or InvitedEmail for invitation {InvitationId}", invitation?.Id);
                return;
            }

            var emailSent = await _govUkNotifyService.SendEmailAsync(
                inviter_emailId,
                _notificationSettings.NetworkManagerAcceptedToRPTemplateId,
                new Dictionary<string, dynamic>
                {
                    { "full_name", inviter_full_name },
                    { "org_name", orgName ?? string.Empty },
                    { "invitee_full_name", StringFormatter.ToTitleCaseSingleWord(invitation.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(invitation.LastName) }
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for InviterUserId {UserId}", MaskEmail(invitation.InvitedEmail), invitation.InviterUserId);
        }
    }
}
