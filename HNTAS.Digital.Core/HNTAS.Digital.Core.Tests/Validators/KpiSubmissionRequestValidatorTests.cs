using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Validators.Arms;
using ElementType = HNTAS.Core.Api.Enums.HeatNetworkElementType;

namespace HNTAS.Digital.Core.Tests.Validators
{
    public class KpiSubmissionRequestValidatorTests
    {
        private readonly KpiSubmissionRequestValidator _validator = new();

        private KpiSubmissionRequest CreateValidRequest()
        {
            return new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "HN2000001",
                    PeriodStart = "2026-01",
                    SourceSystem = "TestSystem"
                },
                Elements = new List<NetworkElementRequest>
                {
                    new NetworkElementRequest
                    {
                        Type = ElementType.EnergyCentre.ToString(),
                        ElementId = "00001",
                        Kpis = new Dictionary<string, KpiValueRequest>
                        {
                            ["EC-KPI-01"] = new KpiValueRequest { Value = 10 }
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task Should_Pass_When_Request_Is_Valid()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Should_Fail_When_Metadata_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.MetaData.NetworkId = ""; // invalid

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("must not be empty"));
        }

        [Fact]
        public async Task Should_Fail_When_Metadata_Null()
        {
            // Arrange
            var request = CreateValidRequest();
            request.MetaData = null;

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Should_Fail_When_Elements_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Elements[0].ElementId = "123"; // invalid

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_ELEMENT_ID");
        }

        [Fact]
        public async Task Should_Fail_When_Elements_Empty()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Elements = new List<NetworkElementRequest>();

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "EMPTY_SUBMISSION");
        }

        [Fact]
        public async Task Should_Fail_When_Invalid_Kpi_For_Element()
        {
            // Arrange
            var request = CreateValidRequest();

            request.Elements[0].Kpis["EC-KPI-99"] = new KpiValueRequest
            {
                Value = 1
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_KPI_FOR_TYPE");
        }

        [Fact]
        public async Task Should_Fail_When_Both_Metadata_And_Elements_Invalid()
        {
            // Arrange
            var request = CreateValidRequest();

            request.MetaData.NetworkId = "";       // metadata fail
            request.Elements[0].ElementId = "123"; // elements fail

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);

            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("must not be empty"));

            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_ELEMENT_ID");
        }


        [Fact]
        public async Task Should_Throw_When_Request_Is_Null()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _validator.ValidateAsync((KpiSubmissionRequest)null)
            );
        }
    }
}