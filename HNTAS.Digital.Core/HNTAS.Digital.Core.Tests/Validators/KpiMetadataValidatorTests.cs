using FluentValidation.TestHelper;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Validators.Arms;

namespace HNTAS.Digital.Core.Tests.Validators
{
    public class KpiMetadataValidatorTests
    {
        private readonly KpiMetadataValidator _validator = new();

        private static KpiMetadata ValidModel() => new()
        {
            NetworkId = "HN2000001",
            PeriodStart = "2026-02",
            SourceSystem = "HNTAS"
        };

        [Fact]
        public void NetworkId_ShouldFail_WhenEmpty()
        {
            var model = ValidModel();
            model.NetworkId = "";

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.NetworkId);
        }

        [Theory]
        [InlineData("HN123")]        // too short
        [InlineData("HN12345678")]  // too long
        [InlineData("hn2000001")]   // lowercase
        [InlineData("AB2000001")]   // wrong prefix
        [InlineData("HNA000001")]   // letters
        public void NetworkId_ShouldFail_WhenFormatInvalid(string networkId)
        {
            var model = ValidModel();
            model.NetworkId = networkId;

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.NetworkId)
                  .WithErrorMessage(
                      "Network ID must start with 'HN' followed by 7 digits (e.g., HN2000001).");
        }

        [Fact]
        public void NetworkId_ShouldPass_WhenValid()
        {
            var result = _validator.TestValidate(ValidModel());

            result.ShouldNotHaveValidationErrorFor(x => x.NetworkId);
        }


        [Fact]
        public void PeriodStart_ShouldFail_WhenEmpty()
        {
            var model = ValidModel();
            model.PeriodStart = "";

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.PeriodStart);
        }

        [Theory]
        [InlineData("2026-00")]
        [InlineData("2026-13")]
        [InlineData("26-02")]
        [InlineData("2026/02")]
        [InlineData("202602")]
        public void PeriodStart_ShouldFail_WhenFormatInvalid(string period)
        {
            var model = ValidModel();
            model.PeriodStart = period;

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.PeriodStart)
                  .WithErrorMessage("Period must be in YYYY-MM format.");
        }

        [Fact]
        public void PeriodStart_ShouldPass_WhenValid()
        {
            var result = _validator.TestValidate(ValidModel());

            result.ShouldNotHaveValidationErrorFor(x => x.PeriodStart);
        }


        [Fact]
        public void SourceSystem_ShouldFail_WhenEmpty()
        {
            var model = ValidModel();
            model.SourceSystem = "";

            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.SourceSystem)
                  .WithErrorMessage("Source system is required.");
        }

        [Fact]
        public void SourceSystem_ShouldPass_WhenProvided()
        {
            var result = _validator.TestValidate(ValidModel());

            result.ShouldNotHaveValidationErrorFor(x => x.SourceSystem);
        }
    }
}
