using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using HNTAS.Digital.Core.Tests.Stub;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Digital.Core.Tests.Services
{

    public class GovUkNotifyServiceTests
    {
        private readonly Mock<ILogger<GovUkNotifyService>> _loggerMock;

        public GovUkNotifyServiceTests()
        {
            _loggerMock = new Mock<ILogger<GovUkNotifyService>>();
        }


        private GovUkNotifyService CreateService(INotificationClientWrapper client)
        {
            return new GovUkNotifyService(
                _loggerMock.Object,
                client);
        }


        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            var service = CreateService(new StubNotificationClient());

            Assert.NotNull(service);
        }


        [Fact]
        public async Task SendEmailAsync_ShouldThrow_WhenEmailIsEmpty()
        {
            var service = CreateService(new StubNotificationClient());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendEmailAsync("", "template-id"));
        }

        [Fact]
        public async Task SendEmailAsync_ShouldThrow_WhenTemplateIdIsEmpty()
        {
            var service = CreateService(new StubNotificationClient());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendEmailAsync("test@test.com", ""));
        }

        [Fact]
        public async Task SendEmailAsync_ShouldReturnTrue_WhenSendSucceeds()
        {
            var service = CreateService(new StubNotificationClient());

            var result = await service.SendEmailAsync(
                "test@test.com",
                "template-id");

            Assert.True(result);
        }


        [Fact]
        public async Task SendEmailAsync_ShouldEnterGenericExceptionCatch()
        {
            // Arrange
            var stub = new StubNotificationClient
            {
                ThrowGeneralException = true
            };

            var service = new GovUkNotifyService(
                Mock.Of<ILogger<GovUkNotifyService>>(),
                stub);

            // Act
            var result = await service.SendEmailAsync(
                "test@test.com",
                "template-id");

            // Assert
            Assert.False(result);
        }


        [Fact]
        public async Task SendEmailAsync_ShouldReturnFalse_WhenNotifyExceptionThrown()
        {
            var service = CreateService(new StubNotificationClient
            {
                ThrowNotifyClientException = true
            });

            var result = await service.SendEmailAsync(
                "test@test.com",
                "template-id");

            Assert.False(result);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldCoverOptionalParameters()
        {
            var service = CreateService(new StubNotificationClient());

            var result = await service.SendEmailAsync(
                emailAddress: "test@test.com",
                templateId: "template-id",
                personalisation: new Dictionary<string, dynamic>
                {
                  { "name", "Test User" }
                },
                reference: "ref-123"
            );

            Assert.True(result);
        }

    }
}
