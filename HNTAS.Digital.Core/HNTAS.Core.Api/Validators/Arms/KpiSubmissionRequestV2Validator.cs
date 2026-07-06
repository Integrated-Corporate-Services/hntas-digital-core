using FluentValidation;
using HNTAS.Core.Api.Models.Arms.V2;
using System.Diagnostics.CodeAnalysis;


namespace HNTAS.Core.Api.Validators.Arms
{
    [ExcludeFromCodeCoverage]
    public class KpiSubmissionRequestV2Validator : AbstractValidator<KpiSubmissionRequestV2>
    {
        public KpiSubmissionRequestV2Validator()
        {
            // Validate the nested metadata object
            RuleFor(x => x.MetaData)
              .NotNull()
              .SetValidator(new KpiMetadataValidator());

            RuleFor(x => x).SetValidator(new KpiElementsValidator());

            RuleFor(x => x).SetValidator(new KpiCarbonInputsValidator());
        }
    }
}

