using HNTAS.Core.Api.Interfaces;
using Notify.Client;
using Notify.Models.Responses;

namespace HNTAS.Core.Api.Services
{
    public class NotificationClientService : INotificationClientWrapper
    {
        private readonly NotificationClient _client;

        public NotificationClientService(string apiKey)
        {
            _client = new NotificationClient(apiKey);
        }

        public Task<EmailNotificationResponse> SendEmailAsync(
                string emailAddress,
                string templateId,
                Dictionary<string, dynamic>? personalisation = null)
        {
            return _client.SendEmailAsync(emailAddress, templateId, personalisation);
        }
    }
}
