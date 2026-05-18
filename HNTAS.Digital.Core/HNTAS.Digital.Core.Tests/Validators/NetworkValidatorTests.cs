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
                new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre.ToString() }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WhenTypeIsWrong_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            var elementId = "00001";
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = elementId, Type = HeatNetworkElementType.Substation.ToString() }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            // Check the Code and Message properties of the KpiSubmissionApiError objects
            Assert.Contains(result.Errors, e => e.Code == "ELEMENT_TYPE_MISMATCH");
            Assert.Contains(result.Errors, e => e.Message.Contains("Expected 'EnergyCentre'"));
            Assert.Contains(result.Errors, e => e.Message.Contains("received 'Substation'"));
        }

        [Fact]
        public async Task ValidateAsync_WhenIdDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "99989", Type = HeatNetworkElementType.EnergyCentre.ToString() }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == "ELEMENT_NOT_FOUND");
            Assert.Equal("99989", result.Errors.First(e => e.Code == "ELEMENT_NOT_FOUND").ElementId);
        }

        private HeatNetwork GetMockNetwork(string hnid)
        {
            return new HeatNetwork
            {
                HnId = hnid,
                NetworkElements = new NetworkElements // Changed to match common naming convention
                {
                    ElementsGroup = new List<ElementGroup>
                {
                    new() { ElementType = "00001", ElementDisplayType = HeatNetworkElementType.EnergyCentre },
                    new() { ElementType = "00003", ElementDisplayType = HeatNetworkElementType.Substation }
                }
                }
            };
        }
    }
}
