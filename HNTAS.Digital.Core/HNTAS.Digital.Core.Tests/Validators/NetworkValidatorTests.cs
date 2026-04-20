using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Validators.Arms;
using Moq;

namespace HNTAS.Digital.Core.Tests.Validators
{
    public class NetworkValidatorTests
    {
        private readonly Mock<IHeatNetworkService> _mockService;
        private readonly HeatNetworkValidator _validator;

        public NetworkValidatorTests()
        {
            _mockService = new Mock<IHeatNetworkService>();
            _validator = new HeatNetworkValidator(_mockService.Object);
        }

        [Fact]
        public async Task ValidateAsync_WhenAllElementsMatch_ReturnsSuccess()
        {
            // Arrange
            var hnid = "HN400219";
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_WhenTypeIsWrong_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            // ID 00001 is an EnergyCentre in the DB, but we send it as a Substation
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "00001", Type = HeatNetworkElementType.Substation }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Expected EnergyCentre, found Substation", result.Message);
        }

        [Fact]
        public async Task ValidateAsync_WhenIdDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "99999", Type = HeatNetworkElementType.EnergyCentre }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("99999 (Not found)", result.Message);
        }

        private HeatNetwork GetMockNetwork(string hnid)
        {
            return new HeatNetwork
            {
                HnId = hnid,
                NetworkElements = new NetworkElements
                {
                    Elements = new List<Element>
                    {
                        new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre },
                        new() { ElementId = "00003", Type = HeatNetworkElementType.Substation }
                    }
                }
            };
        }
    }
}
