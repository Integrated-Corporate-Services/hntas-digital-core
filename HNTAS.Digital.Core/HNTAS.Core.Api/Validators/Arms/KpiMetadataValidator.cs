using FluentValidation;
using HNTAS.Core.Api.Data.Models.Arms.Submission;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiMetadataValidator : AbstractValidator<KpiMetadata>
    {
        public KpiMetadataValidator()
        {
            RuleFor(x => x.NetworkId)
                .NotEmpty()
                .Matches(@"^HN[0-9]{7}$")
                .WithMessage("Network ID must start with 'HN' followed by 7 digits (e.g., HN2000001).");

            RuleFor(x => x.PeriodStart)
                .NotEmpty()
                .Matches(@"^[0-9]{4}-(0[1-9]|1[0-2])$")
                .WithMessage("Period must be in YYYY-MM format.");

            RuleFor(x => x.SourceSystem)
                .NotEmpty()
                .WithMessage("Source system is required.");
        }
    }
}
