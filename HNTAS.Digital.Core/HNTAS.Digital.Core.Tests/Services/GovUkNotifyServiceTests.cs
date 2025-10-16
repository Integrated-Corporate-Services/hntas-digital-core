using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Notify.Client;


namespace HNTAS.Digital.Core.Tests.Services
{
    public class GovUkNotifyServiceTests
    {
        private readonly IGovUkNotifyService _notifyService;
        private readonly NotificationClient _notificationClient;
        private readonly string _apiKey;
        private readonly string _templateId = "297e670f-d6c8-49f2-b0d7-abe77256318a";

        public GovUkNotifyServiceTests()
        {
            var httpClient = new HttpClient();
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();

            _apiKey = Environment.GetEnvironmentVariable("GOV_NOTIFY_API_KEY") ?? throw new ArgumentNullException(
                "GOV.UK Notify API key 'GovUkNotify:ApiKey' is not configured.");
            _notificationClient = new NotificationClient(_apiKey);

            _notifyService = CreateService(_apiKey);
        }

        private GovUkNotifyService CreateService(string? apiKey)
        {
            var config = new ConfigurationBuilder().Build();
            var logger = new Mock<ILogger<GovUkNotifyService>>().Object;

            return new GovUkNotifyService(config, logger);
        }

        [Fact]
        public async Task SendNotification_ReturnsSuccess_WhenDataIsValid()
        {
            // Arrange
            var email = "test@mailinator.com";
            var personalisation = new Dictionary<string, dynamic>
            {
                { "fullName", "Test User" },
                { "orgName", "Test Org"},
                { "orgId", "test-org-id" },
                { "address", "some address" }
            };
            var service = CreateService(_apiKey);

            // Act
            var result = await service.SendEmailAsync(email, _templateId, personalisation);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SendNotification_ThrowsException_WhenApiKeyIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GOVUK_NOTIFY_API_KEY", null);
            var httpClient = new HttpClient();
            var config = new ConfigurationBuilder().Build();
            var service = CreateService(_apiKey);

            // Act & Assert
            var result = await service.SendEmailAsync("template-id", "test@example.com", new Dictionary<string, dynamic>());
            Assert.False(result);
        }
    }
}