using Notify.Models.Responses;

namespace HNTAS.Core.Api.Interfaces
{
    public interface INotificationClientWrapper
    {
        public Task<EmailNotificationResponse> SendEmailAsync(
               string emailAddress,
               string templateId,
               Dictionary<string, dynamic>? personalisation = null);
    }
}
