using HNTAS.Core.Api.Interfaces;
using Notify.Models.Responses;

namespace HNTAS.Digital.Core.Tests.Stub
{
    public class StubNotificationClient : INotificationClientWrapper
    {

        public bool ThrowNotifyClientException { get; set; }
        public bool ThrowGeneralException { get; set; }

        public async Task<EmailNotificationResponse> SendEmailAsync(
            string emailAddress,
            string templateId,
            Dictionary<string, dynamic>? personalisation = null)
        {
            // Force async boundary
            await Task.Yield();

            if (ThrowNotifyClientException)
            {
                throw new Notify.Exceptions.NotifyClientException(
                    "Notify client error",
                    400,
                    null);
            }

            if (ThrowGeneralException)
            {
                // PURE System.Exception
                throw new Exception("Unexpected failure");
            }

            return new EmailNotificationResponse
            {
                id = Guid.NewGuid().ToString()
            };
        }

    }
}
