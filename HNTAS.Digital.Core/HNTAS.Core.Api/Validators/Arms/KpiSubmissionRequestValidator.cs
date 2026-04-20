using FluentValidation;
using HNTAS.Core.Api.Models.Arms;
using ElementType = HNTAS.Core.Api.Enums.HeatNetworkElementType;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiSubmissionRequestValidator : AbstractValidator<KpiSubmissionRequest>
    {

        private static readonly Dictionary<ElementType, HashSet<string>> AllowedKpisByElement = new()
        {
            { ElementType.EnergyCentre, new HashSet<string> {
                "EC-KPI-01", "EC-KPI-02", "EC-KPI-03", "EC-KPI-04", "EC-KPI-05", "EC-KPI-06", "EC-KPI-07", "EC-KPI-08",
                "EC-KPI-09A", "EC-KPI-09B", "EC-KPI-10A", "EC-KPI-10B", "EC-KPI-11", "EC-KPI-12", "EC-KPI-13",
                "EC-KPI-14", "EC-KPI-15", "EC-KPI-16A", "EC-KPI-16B", "EC-KPI-16C", "EC-KPI-16D", "EC-KPI-16E",
                "EC-KPI-16F", "EC-KPI-17A", "EC-KPI-17B", "EC-KPI-17C", "EC-KPI-17D", "EC-KPI-17E", "EC-KPI-17F", "EC-KPI-18"
            }},
            { ElementType.DistrictDistribution, new HashSet<string> {
                "DD-KPI-01", "DD-KPI-02", "DD-KPI-03", "DD-KPI-04", "DD-KPI-05", "DD-KPI-06", "DD-KPI-07", "DD-KPI-08", "DD-KPI-09", "DD-KPI-10"
            }},
            { ElementType.Substation, new HashSet<string> {
                "SS-KPI-01", "SS-KPI-02", "SS-KPI-03", "SS-KPI-04", "SS-KPI-05", "SS-KPI-06", "SS-KPI-07", "SS-KPI-08",
                "SS-KPI-09A", "SS-KPI-09B", "SS-KPI-10A", "SS-KPI-10B", "SS-KPI-11", "SS-KPI-12", "SS-KPI-13",
                "SS-KPI-14", "SS-KPI-15", "SS-KPI-16", "SS-KPI-17"
            }},
            { ElementType.CommunalDistribution, new HashSet<string> {
            "CD-KPI-01", "CD-KPI-02", "CD-KPI-03", "CD-KPI-04", "CD-KPI-05", "CD-KPI-06", "CD-KPI-07", "CD-KPI-08", "CD-KPI-09"
            }},
            { ElementType.ConsumerConnection, new HashSet<string> {
                "CC-KPI-04", "CC-KPI-05", "CC-KPI-06", "CC-KPI-07"
            }}
        };

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

            //Validate KPI’s are submitted under their respective elements
            RuleForEach(x => x.Elements).ChildRules(element =>
            {
                element.RuleForEach(e => e.Kpis).Custom((kpiEntry, context) =>
                {
                    var networkElement = context.InstanceToValidate as NetworkElementRequest;
                    if (networkElement == null) return;

                    var kpiKey = kpiEntry.Key;
                    var elementType = networkElement.Type;

                    // This single check validates BOTH the prefix and the specific KPI ID
                    if (AllowedKpisByElement.TryGetValue(elementType, out var allowedKeys))
                    {
                        if (!allowedKeys.Contains(kpiKey))
                        {
                            // If it's not in the list, it's a failure. 
                            // We can even tell them what prefix we expected to be helpful.
                            var expectedPrefix = elementType.ToString().Substring(0, 2).ToUpper();

                            context.AddFailure(
                                $"Kpis[{kpiKey}]",
                                $"Invalid KPI ID '{kpiKey}' for {elementType}. " +
                                $"KPIs for this element must be from the allowed spec list (starting with {expectedPrefix}-)."
                            );
                        }
                    }
                });
            });

            RuleFor(x => x).Custom((request, context) =>
            {
                bool hasConsumerElements = request.Elements?.Any(e => e.Type == ElementType.ConsumerConnection) ?? false;
                bool hasAggregatedData = request.ConsumerConnectionAggregatedKpis?.Any() ?? false;

                // Check A: Aggregated data exists but no elements found
                if (!hasConsumerElements && hasAggregatedData)
                {
                    context.AddFailure("ConsumerConnectionAggregatedKpis", "Aggregated KPIs (CC-KPI-01 to 03) must not be included if no 'ConsumerConnection' elements are present.");
                }

                // Check B: Elements found but no aggregated data provided
                if (hasConsumerElements && !hasAggregatedData)
                {
                    context.AddFailure("ConsumerConnectionAggregatedKpis", "Aggregated KPIs (CC-KPI-01 to 03) are mandatory when 'ConsumerConnection' elements are present.");
                }

                // Check C: Validate specific keys in the Aggregated section
                if (hasAggregatedData)
                {
                    var requiredKeys = new[] { "CC-KPI-01", "CC-KPI-02", "CC-KPI-03" };
                    var submittedKeys = request.ConsumerConnectionAggregatedKpis.Keys;

                    foreach (var key in requiredKeys)
                    {
                        if (!submittedKeys.Contains(key))
                        {
                            context.AddFailure("ConsumerConnectionAggregatedKpis", $"Missing mandatory aggregated KPI: {key}");
                        }
                    }

                    var invalidKeys = submittedKeys.Where(k => !requiredKeys.Contains(k));
                    foreach (var extra in invalidKeys)
                    {
                        context.AddFailure("ConsumerConnectionAggregatedKpis", $"KPI '{extra}' should be reported per-element, not in the aggregated section.");
                    }
                }
            });
        }
    }
}
