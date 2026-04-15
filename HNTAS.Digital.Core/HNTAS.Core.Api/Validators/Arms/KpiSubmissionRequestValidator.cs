using FluentValidation;
using HNTAS.Core.Api.Models.Arms;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiSubmissionRequestValidator : AbstractValidator<KpiSubmissionRequest>
    {
        public KpiSubmissionRequestValidator()
        {
            // Validate the nested metadata object
            RuleFor(x => x.MetaData).SetValidator(new KpiMetadataValidator());

            // elements array must have >= 1 items
            RuleFor(x => x.Elements)
                .NotEmpty()
                .WithMessage("The 'elements' array must contain at least one item.")
                .Must(list => list.Count >= 1);

            RuleForEach(x => x.Elements).ChildRules(element =>
            {
                // Validate the 5-digit ID (Matches Image 3)
                element.RuleFor(e => e.ElementId)
                    .NotEmpty()
                    .Matches(@"^\d{5}$")
                    .WithMessage("Element ID must be exactly 5 digits (e.g., 00001).");

                // Validate the Enum Type
                element.RuleFor(e => e.Type)
                    .IsInEnum()
                    .WithMessage("Invalid element type provided.");

                // Validate that KPIs are actually provided
                element.RuleFor(e => e.Kpis)
                    .NotEmpty()
                    .WithMessage("Each element must have at least one KPI reported.");
            });
        }
    }
}
