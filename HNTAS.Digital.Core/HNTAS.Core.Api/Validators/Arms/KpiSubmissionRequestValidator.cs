using FluentValidation;
using HNTAS.Core.Api.Models.Arms;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Validators.Arms
{
    [ExcludeFromCodeCoverage]
    public class KpiSubmissionRequestValidator : AbstractValidator<KpiSubmissionRequest>
    {
        public KpiSubmissionRequestValidator()
        {
            RuleFor(x => x.MetaData)
                .NotNull()
                .SetValidator(new KpiMetadataValidator());

            RuleFor(x => x).SetValidator(new KpiElementsValidator());
        }
    }
}
