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
        public async Task ValidateAsync_WhenAllElementCountsMatchExactly_ReturnsSuccess()
        {
            // Arrange
            var hnid = "HN400219";
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre.ToString() },
                new() { ElementId = "00002", Type = HeatNetworkElementType.Substation.ToString() },
                new() { ElementId = "00003", Type = HeatNetworkElementType.Substation.ToString() }
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
        public async Task ValidateAsync_WhenElementCountMismatches_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            // Expected registry counts from mock data: EnergyCentre = 1, Substation = 2
            // Act: Sending only 1 Substation instead of 2
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre.ToString() },
                new() { ElementId = "00002", Type = HeatNetworkElementType.Substation.ToString() }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.Errors);

            var error = Assert.Single(result.Errors);
            Assert.Equal("ELEMENT_COUNT_NOT_MATCHED", error.Code);
            Assert.Contains("Element count mismatch for type 'Substation'", error.Message);
            Assert.Contains("Expected '2', but received '1'", error.Message);
            Assert.Null(error.ElementId); // Verified that ElementId remains null for count mismatches
        }

        [Fact]
        public async Task ValidateAsync_WhenPayloadContainsDuplicateElementIds_ReturnsFailure()
        {
            // Arrange
            var hnid = "HN400219";
            // Act: Sending duplicate elementId "00002"
            var request = new List<NetworkElementRequest>
            {
                new() { ElementId = "00001", Type = HeatNetworkElementType.EnergyCentre.ToString() },
                new() { ElementId = "00002", Type = HeatNetworkElementType.Substation.ToString() },
                new() { ElementId = "00002", Type = HeatNetworkElementType.Substation.ToString() }
            };

            _mockService.Setup(s => s.GetByHnIdAsync(hnid))
                .ReturnsAsync(GetMockNetwork(hnid));

            // Act
            var result = await _validator.ValidateAsync(hnid, request);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.Errors);

            // Should catch the DUPLICATE_ELEMENT_ID error
            Assert.Contains(result.Errors, e => e.Code == "DUPLICATE_ELEMENT_ID");
            var dupError = result.Errors.First(e => e.Code == "DUPLICATE_ELEMENT_ID");
            Assert.Contains("contains duplicate element ID '00002'", dupError.Message);
        }

        private HeatNetwork GetMockNetwork(string hnid)
        {
            return new HeatNetwork
            {
                HnId = hnid,
                NetworkElements = new NetworkElements
                {
                    ElementsGroup = new List<ElementGroup>
                    {
                        // Setting up targeted baseline counts for testing
                        new() { Count = 1, ElementDisplayType = HeatNetworkElementType.EnergyCentre },
                        new() { Count = 2, ElementDisplayType = HeatNetworkElementType.Substation }
                    }
                }
            };
        }
    }
}