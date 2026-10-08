using HNTAS.Core.Api.Interfaces;
using Notify.Models.Responses;

namespace HNTAS.Core.Api.Services
{
    public class GovUkNotifyService : IGovUkNotifyService
    {
        private readonly INotificationClientWrapper _notificationClient;
        private readonly ILogger<GovUkNotifyService> _logger;

        /// <summary>
        /// Initializes a new instance of the GovUkNotifyService.
        /// </summary>
        public GovUkNotifyService(ILogger<GovUkNotifyService> logger, INotificationClientWrapper notificationClient)
        {
            _notificationClient = notificationClient;
            _logger = logger;
        }
        /// <summary>
        /// Sends an email using the GOV.UK Notify API.
        /// </summary>
        /// <param name="emailAddress">The recipient's email address.</param>
        /// <param name="templateId">The ID of the email template to use.</param>
        /// <param name="personalisation">A dictionary of personalization fields for the template.</param>
        /// <param name="reference">An optional unique reference for this notification.</param>
        /// <returns>True if the email was sent successfully, false otherwise.</returns>
        public async Task<bool> SendEmailAsync(
            string emailAddress,
            string templateId,
            Dictionary<string, dynamic>? personalisation = null,
            string? reference = null)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                _logger.LogWarning("Attempted to send email with null or empty email address for template {TemplateId}.", templateId);
                throw new ArgumentException("Email address cannot be null or empty.", nameof(emailAddress));
            }
            if (string.IsNullOrEmpty(templateId))
            {
                _logger.LogWarning("Attempted to send email with null or empty template ID to {EmailAddress}.", emailAddress);
                throw new ArgumentException("Template ID cannot be null or empty.", nameof(templateId));
            }

            _logger.LogInformation("Attempting to send email to {EmailAddress} using template {TemplateId}.", emailAddress, templateId);

            try
            {
                EmailNotificationResponse response = await _notificationClient.SendEmailAsync(
                    emailAddress: emailAddress,
                    templateId: templateId,
                    personalisation: personalisation
                );

                _logger.LogInformation("Email sent successfully to {EmailAddress}. Notification ID: {NotificationId}. Reference: {Reference}",
                    emailAddress, response.id, reference ?? "N/A");
                return true;
            }
            catch (Notify.Exceptions.NotifyClientException ex)
            {
                _logger.LogError(ex, "GOV.UK Notify client error sending email to {EmailAddress} using template {TemplateId}. Error: {ErrorMessage}",
                    emailAddress, templateId, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while sending email to {EmailAddress} using template {TemplateId}. Error: {ErrorMessage}",
                    emailAddress, templateId, ex.Message);
                return false;
            }
        }
    }
}
