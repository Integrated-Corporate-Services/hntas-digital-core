using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;


namespace HNTAS.Digital.Core.Tests.Services
{
    public class GovUkNotifyServiceTests
    {
        
        private readonly string _templateId = "297e670f-d6c8-49f2-b0d7-abe77256318a"; // A valid OrgCreatedEmailTemplateId template, personalisation obj for positive testcase has to change if this changes

        private GovUkNotifyService CreateService()
        {
            return new GovUkNotifyService(new ConfigurationBuilder().Build(), new Mock<ILogger<GovUkNotifyService>>().Object);
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
            var service = CreateService();

            // Act & Assert
            var result = await service.SendEmailAsync(email, _templateId, personalisation);            
            Assert.True(result);
        }

        [Fact]
        public async Task SendNotification_ThrowsException_WhenApiKeyIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GOVUK_NOTIFY_API_KEY", null);
            var httpClient = new HttpClient();
            var config = new ConfigurationBuilder().Build();
            var service = CreateService();

            // Act & Assert
            var result = await service.SendEmailAsync("template-id", "test@example.com", new Dictionary<string, dynamic>());
            Assert.False(result);
        }
    }
}