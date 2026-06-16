using FluentValidation;
using FluentValidation.Results;
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


        //private static readonly Dictionary<ElementType, HashSet<string>> MandatoryKpisByElement = new()
        //{
        //    { ElementType.EnergyCentre, new HashSet<string> {
        //        "EC-KPI-01", "EC-KPI-02", "EC-KPI-03", "EC-KPI-04", "EC-KPI-05", "EC-KPI-06", "EC-KPI-07", "EC-KPI-08",
        //        "EC-KPI-11", "EC-KPI-12", "EC-KPI-13", "EC-KPI-14", "EC-KPI-15", "EC-KPI-18"
        //    }},
        //    { ElementType.DistrictDistribution, new HashSet<string> {
        //        "DD-KPI-01", "DD-KPI-02", "DD-KPI-03", "DD-KPI-04", "DD-KPI-05", "DD-KPI-06", "DD-KPI-07", "DD-KPI-08", "DD-KPI-09", "DD-KPI-10"
        //    }},
        //    { ElementType.Substation, new HashSet<string> {
        //        "SS-KPI-01", "SS-KPI-02", "SS-KPI-03", "SS-KPI-04", "SS-KPI-05", "SS-KPI-06", "SS-KPI-07", "SS-KPI-08",
        //        "SS-KPI-11", "SS-KPI-12", "SS-KPI-13", "SS-KPI-14", "SS-KPI-15", "SS-KPI-16", "SS-KPI-17"
        //    }},
        //    { ElementType.CommunalDistribution, new HashSet<string> {
        //        "CD-KPI-01", "CD-KPI-02", "CD-KPI-03", "CD-KPI-04", "CD-KPI-05", "CD-KPI-06", "CD-KPI-07", "CD-KPI-08", "CD-KPI-09"
        //    }},
        //    { ElementType.ConsumerConnection, new HashSet<string> {
        //        "CC-KPI-04", "CC-KPI-05", "CC-KPI-06", "CC-KPI-07"
        //    }}
        //};

        public KpiSubmissionRequestValidator()
        {
            // Validate the nested metadata object
            RuleFor(x => x.MetaData).SetValidator(new KpiMetadataValidator());

            // elements array must have >= 1 items
            RuleFor(x => x.Elements)
                  .NotEmpty()
                  .WithErrorCode("EMPTY_SUBMISSION")
                  .WithMessage("The 'elements' array must contain at least one item.");

            // 1. Validate Individual Element Schema
            RuleForEach(x => x.Elements).ChildRules(element =>
            {
                element.RuleFor(e => e.ElementId)
                    .NotEmpty()
                    .Matches(@"^\d{5}$")
                    .WithErrorCode("INVALID_ELEMENT_ID")
                    .WithMessage("Element ID must be exactly 5 digits (e.g., 00001).")
                    .WithState(e => new { elementId = e.ElementId, kpis = (string)null });

                //element.RuleFor(e => e.Type)
                //    .IsInEnum()
                //    .WithErrorCode("INVALID_ELEMENT_TYPE")
                //    .WithMessage("Invalid element type provided.")
                //    .WithState(e => new { elementId = e.ElementId, kpis = (string)null });

                //element.RuleFor(e => e.Kpis)
                //    .NotEmpty()
                //    .WithErrorCode("MISSING_KPI_DATA")
                //    .WithMessage("Each element must have at least one KPI reported.")
                //    .WithState(e => new { elementId = e.ElementId, kpi = "All" });
            });

            /// Validate KPI’s are submitted under their respective elements
            RuleFor(x => x.Elements).Custom((elements, context) =>
            {
                if (elements == null) return;

                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    bool isValidEnum = Enum.TryParse<ElementType>(element.Type, true, out var elementType);

                    if (!isValidEnum)
                    {
                        // If the string doesn't match any enum member, stop and report the error
                        context.AddFailure(new ValidationFailure($"Elements[{i}].Type", "Invalid element type provided.")
                        {
                            ErrorCode = "INVALID_ELEMENT_TYPE",
                            CustomState = new { elementId = element.ElementId, kpis = (List<string>)null }
                        });
                        continue; // Move to the next element
                    }

                    // 1. Determine the expected prefix
                    var expectedPrefix = elementType switch
                    {
                        ElementType.EnergyCentre => "EC",
                        ElementType.DistrictDistribution => "DD",
                        ElementType.Substation => "SS",
                        ElementType.CommunalDistribution => "CD",
                        ElementType.ConsumerConnection => "CC",
                        _ => "XX"
                    };

                    // 2. Validate individual KPIs
                    if (AllowedKpisByElement.TryGetValue(elementType, out var allowedKeys))
                    {

                        var invalidKeys = element.Kpis.Keys
                                    .Where(kpiKey => !allowedKeys.Contains(kpiKey))
                                    .ToList();

                        if (invalidKeys.Any())
                        {
                            // 2. Build the path (pointing to the collection)
                            var propertyPath = $"Elements[{i}].Kpis";

                            var failure = new ValidationFailure(propertyPath,
                                $"One or more KPI IDs are invalid for {elementType}. Must start with {expectedPrefix}-.")
                            {
                                ErrorCode = "INVALID_KPI_FOR_TYPE",
                                CustomState = new
                                {
                                    elementId = element.ElementId,
                                    // 3. Pass the entire list of bad keys
                                    kpis = invalidKeys
                                }
                            };

                            context.AddFailure(failure);
                        }
                    }
                }
            });

            RuleFor(x => x).Custom((request, context) =>
            {
                var requiredKeys = new[] { "CC-KPI-01", "CC-KPI-02", "CC-KPI-03" };
                var aggregatedKpis = request.ConsumerConnectionAggregatedKpis ?? new Dictionary<string, KpiValueAggregatedRequest>();

                bool hasConsumerElements = request.Elements?.Any(e => e.Type == ElementType.ConsumerConnection.ToString()) ?? false;
                bool hasAggregatedData = aggregatedKpis.Any();

                // 1. Check for UNEXPECTED data
                if (!hasConsumerElements && hasAggregatedData)
                {
                    context.AddFailure(new ValidationFailure("ConsumerConnectionAggregatedKpis", "Aggregated KPIs must not be included if no 'ConsumerConnection' elements are present.")
                    {
                        ErrorCode = "UNEXPECTED_AGGREGATED_DATA",
                        CustomState = new
                        {
                            elementId = "Aggregated",
                            kpis = aggregatedKpis.Keys.ToList() // Grouped list of unexpected keys
                        }
                    });
                }

                // 2. Check for MISSING data (GROUPED VERSION)
                if (hasConsumerElements)
                {
                    var missingKeys = requiredKeys.Where(key => !aggregatedKpis.ContainsKey(key)).ToList();

                    if (missingKeys.Any())
                    {
                        context.AddFailure(new ValidationFailure("ConsumerConnectionAggregatedKpis", "Missing mandatory aggregated KPIs.")
                        {
                            ErrorCode = "MISSING_MANDATORY_KPI",
                            CustomState = new
                            {
                                elementId = "Aggregated",
                                kpis = missingKeys // This is your List<string>
                            }
                        });
                    }
                }

                // 3. Check for INVALID keys (GROUPED VERSION)
                if (hasAggregatedData)
                {
                    var invalidKeys = aggregatedKpis.Keys.Where(k => !requiredKeys.Contains(k)).ToList();

                    if (invalidKeys.Any())
                    {
                        context.AddFailure(new ValidationFailure("ConsumerConnectionAggregatedKpis", "Invalid KPIs found in aggregated section.")
                        {
                            ErrorCode = "INVALID_AGGREGATED_KPI",
                            CustomState = new
                            {
                                elementId = "Aggregated",
                                kpis = invalidKeys
                            }
                        });
                    }
                }
            });

            RuleFor(x => x.Elements).Custom((elements, context) =>
            {
                if (elements == null) return;

                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    var submittedKeys = element.Kpis.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    string path = $"Elements[{i}].Kpis";
                    Enum.TryParse<ElementType>(element.Type, true, out var elementType);

                    // 1. Mandatory KPI Check (GROUPED)
                    //if (MandatoryKpisByElement.TryGetValue(elementType, out var requiredSet))
                    //{
                    //    var missing = requiredSet.Where(req => !submittedKeys.Contains(req)).ToList();

                    //    if (missing.Any())
                    //    {
                    //        context.AddFailure(new ValidationFailure(path, "Missing mandatory KPIs for this element.")
                    //        {
                    //            ErrorCode = "MISSING_MANDATORY_KPI",
                    //            // Using 'kpis' (plural) to match your grouped model
                    //            CustomState = new { elementId = element.ElementId, kpis = missing }
                    //        });
                    //    }
                    //}

                    // 2. Business Rules (These helpers should also be updated to return lists in CustomState)
                    //if (element.Type == ElementType.EnergyCentre.ToString())
                    //{
                    //    ValidateExclusivity(element, submittedKeys, context, i, "EC-KPI-09A", "EC-KPI-09B");
                    //    ValidateExclusivity(element, submittedKeys, context, i, "EC-KPI-10A", "EC-KPI-10B");

                    //    var group16 = new[] { "EC-KPI-16A", "EC-KPI-16B", "EC-KPI-16C", "EC-KPI-16D", "EC-KPI-16E", "EC-KPI-16F" };
                    //    ValidateAtLeastOne(element, submittedKeys, context, i, "EC-KPI-16", group16);

                    //    var group17 = new[] { "EC-KPI-17A", "EC-KPI-17B", "EC-KPI-17C", "EC-KPI-17D", "EC-KPI-17E", "EC-KPI-17F" };
                    //    ValidateAtLeastOne(element, submittedKeys, context, i, "EC-KPI-17", group17);
                    //}
                    //else if (element.Type == ElementType.Substation.ToString())
                    //{
                    //    ValidateExclusivity(element, submittedKeys, context, i, "SS-KPI-09A", "SS-KPI-09B");
                    //    ValidateExclusivity(element, submittedKeys, context, i, "SS-KPI-10A", "SS-KPI-10B");
                    //}
                }
            });
        }


        private void ValidateExclusivity(NetworkElementRequest element, HashSet<string> submitted, ValidationContext<KpiSubmissionRequest> context, int index, string kpiA, string kpiB)
        {
            var path = $"Elements[{index}]";
            bool hasA = submitted.Contains(kpiA);
            bool hasB = submitted.Contains(kpiB);

            // 1. Both provided (Mutual Exclusivity)
            if (hasA && hasB)
            {
                context.AddFailure(new ValidationFailure(path, $"Reported both '{kpiA}' and '{kpiB}', but only one is allowed.")
                {
                    ErrorCode = "MUTUALLY_EXCLUSIVE_KPI",
                    CustomState = new
                    {
                        elementId = element.ElementId,
                        kpis = new List<string> { kpiA, kpiB } // Grouped list
                    }
                });
            }
            // 2. Neither provided (Mandatory Choice)
            else if (!hasA && !hasB)
            {
                context.AddFailure(new ValidationFailure(path, $"Either '{kpiA}' or '{kpiB}' must be reported.")
                {
                    ErrorCode = "MISSING_MANDATORY_KPI",
                    CustomState = new
                    {
                        elementId = element.ElementId,
                        kpis = new List<string> { kpiA, kpiB } // Grouped list
                    }
                });
            }
        }

        private void ValidateAtLeastOne(NetworkElementRequest element, HashSet<string> submitted, ValidationContext<KpiSubmissionRequest> context, int index, string groupName, string[] groupKeys)
        {
            // Only apply group mandatory checks if the type is Energy Centre
            if (element.Type == ElementType.EnergyCentre.ToString() && !groupKeys.Any(k => submitted.Contains(k)))
            {
                var path = $"Elements[{index}]";
                context.AddFailure(new ValidationFailure(path, $"At least one KPI from the group '{groupKeys.First()}' to '{groupKeys.Last()}' must be reported.")
                {
                    ErrorCode = "MISSING_MANDATORY_GROUP",
                    CustomState = new
                    {
                        elementId = element.ElementId,
                        // Return the whole group so the frontend knows which fields to highlight
                        kpis = groupKeys.ToList()
                    }
                });
            }
        }
    }
}
