using FluentValidation;
using FluentValidation.Results;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using ElementType = HNTAS.Core.Api.Enums.HeatNetworkElementType;


namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiSubmissionRequestV2Validator : AbstractValidator<KpiSubmissionRequestV2>
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

        public KpiSubmissionRequestV2Validator()
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

            // ==========================================================
            // Root-Level Carbon Calculator Inputs Validation
            // ==========================================================
            RuleFor(x => x).Custom((request, context) =>
            {
                // Only run if there is at least one EnergyCentre present in the collection
                bool hasEnergyCentre = request.Elements?.Any(e => string.Equals(e.Type, ElementType.EnergyCentre.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;
                if (!hasEnergyCentre) return;

                var carbonInputs = request.CarbonInputsV2;
                var elementPath = "carbon_calculator_inputs";

                // Guard Clause: Check if the main collection is null or empty
                if (carbonInputs == null || !carbonInputs.Any())
                {
                    context.AddFailure(new ValidationFailure(elementPath, "Carbon calculator inputs are required when an Energy Centre element exists.")
                    {
                        ErrorCode = "MISSING_CARBON_INPUTS",
                        CustomState = new { elementId = (string)null, kpis = (List<string>)null }
                    });
                    return;
                }

                var allowedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "chp_totals", "hpm_totals", "blr_totals"
                };

                var invalidSections = carbonInputs.Keys.Where(key => !allowedSections.Contains(key)).ToList();
                if (invalidSections.Any())
                {
                    context.AddFailure(new ValidationFailure(elementPath, $"Unexpected sections found in carbon calculator inputs: {string.Join(", ", invalidSections)}.")
                    {
                        ErrorCode = "INVALID_INPUT_SECTION",
                        CustomState = new { elementId = (string)null, kpis = invalidSections }
                    });
                    return; // Stop early if the root structure is corrupted
                }

                // Check that at least one of the major asset total sections is present
                bool hasChp = carbonInputs.TryGetValue("chp_totals", out var chpSection) && chpSection != null && chpSection.Any();
                bool hasHpm = carbonInputs.TryGetValue("hpm_totals", out var hpmSection) && hpmSection != null && hpmSection.Any();
                bool hasBlr = carbonInputs.TryGetValue("blr_totals", out var blrSection) && blrSection != null && blrSection.Any();

                if (!hasChp && !hasHpm && !hasBlr)
                {
                    context.AddFailure(new ValidationFailure(elementPath, "At least one production asset section ('chp_totals', 'hpm_totals', or 'blr_totals') must be provided with valid data entries.")
                    {
                        ErrorCode = "MISSING_ASSET_SECTIONS",
                        CustomState = new { elementId = (string)null, kpis = new List<string> { "chp_totals", "hpm_totals", "blr_totals" } }
                    });
                    return;
                }

                //if (!carbonInputs.TryGetValue("meta_data", out var metaDataSection) || metaDataSection == null || !metaDataSection.ContainsKey("EC-DATA-19"))
                //{
                //    context.AddFailure(new ValidationFailure(elementPath, "The 'meta_data' section with 'EC-DATA-19' is required.")
                //    {
                //        ErrorCode = "MISSING_BACKGROUND_SECTION",
                //        CustomState = new { elementId = (string)null, kpis = new List<string> { "EC-DATA-19" } }
                //    });
                //}
                //else
                //{
                //    // Check for any unauthorized keys inside mata_data
                //    var allowedMetaKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EC-DATA-19" };
                //    var invalidMetaKeys = metaDataSection.Keys.Where(k => !allowedMetaKeys.Contains(k)).ToList();

                //    if (invalidMetaKeys.Any())
                //    {
                //        context.AddFailure(new ValidationFailure($"{elementPath}.mata_data", $"Unexpected keys found in mata_data: {string.Join(", ", invalidMetaKeys)}.")
                //        {
                //            ErrorCode = "INVALID_CARBON_KEY",
                //            CustomState = new { elementId = (string)null, kpis = invalidMetaKeys }
                //        });
                //    }

                //    if (metaDataSection.TryGetValue("EC-DATA-19", out var d19) && d19?.Value != null)
                //    {
                //        ValidateDate(context, null, elementPath, "EC-DATA-19", d19.Value.ToString());
                //    }
                //}

                // B. Validate CHP Totals
                if (hasChp)
                {
                    var chpDates = new[] { "EC-DATA-52" };

                    // 1. Separate mandatory numbers from optional ones
                    var mandatoryChpNumbers = new[] { "EC-DATA-53", "EC-DATA-55", "EC-DATA-57" };
                    var optionalChpNumbers = new[] { "EC-DATA-47" };

                    var allowedChpKeys = chpDates
                                        .Concat(mandatoryChpNumbers)
                                        .Concat(optionalChpNumbers)
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var invalidChpKeys = chpSection!.Keys.Where(k => !allowedChpKeys.Contains(k)).ToList();

                    if (invalidChpKeys.Any())
                    {
                        context.AddFailure(new ValidationFailure($"{elementPath}.chp_totals", $"Unexpected keys found in chp_totals: {string.Join(", ", invalidChpKeys)}.")
                        {
                            ErrorCode = "INVALID_CARBON_KEY",
                            CustomState = new { elementId = (string)null, kpis = invalidChpKeys }
                        });
                    }

                    // 3. Pass both arrays into your method wrapper (update your method signature if required)
                    ValidateSectionFields(context, null, elementPath, "chp_totals", chpSection, chpDates, mandatoryChpNumbers, optionalChpNumbers);
                }

                // C. Validate HPM Totals
                if (hasHpm)
                {
                    var hpmNumbers = new[] { "EC-DATA-66", "EC-DATA-68" };

                    var allowedHpmKeys = hpmNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var invalidHpmKeys = hpmSection!.Keys.Where(k => !allowedHpmKeys.Contains(k)).ToList();

                    if (invalidHpmKeys.Any())
                    {
                        context.AddFailure(new ValidationFailure($"{elementPath}.hpm_totals", $"Unexpected keys found in hpm_totals: {string.Join(", ", invalidHpmKeys)}.")
                        {
                            ErrorCode = "INVALID_CARBON_KEY",
                            CustomState = new { elementId = (string)null, kpis = invalidHpmKeys }
                        });
                    }

                    ValidateSectionFields(context, null, elementPath, "hpm_totals", hpmSection, Array.Empty<string>(), hpmNumbers);
                }

                // D. Validate Boiler Totals
                if (hasBlr)
                {
                    var blrNumbers = new[] { "EC-DATA-84", "EC-DATA-86" };

                    var allowedBlrKeys = blrNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var invalidBlrKeys = blrSection!.Keys.Where(k => !allowedBlrKeys.Contains(k)).ToList();

                    if (invalidBlrKeys.Any())
                    {
                        context.AddFailure(new ValidationFailure($"{elementPath}.blr_totals", $"Unexpected keys found in blr_totals: {string.Join(", ", invalidBlrKeys)}.")
                        {
                            ErrorCode = "INVALID_CARBON_KEY",
                            CustomState = new { elementId = (string)null, kpis = invalidBlrKeys }
                        });
                    }

                    ValidateSectionFields(context, null, elementPath, "blr_totals", blrSection, Array.Empty<string>(), blrNumbers);
                }
            });
        }

        // ==========================================
        // Reusable Type Validation Helper Methods
        // ==========================================
        private static void ValidateSectionFields(
            ValidationContext<KpiSubmissionRequestV2> context,
            string elementId,
            string elementPath,
            string sectionName,
            Dictionary<string, CCKpiValueRequest> section,
            string[] dateKeys,
            string[] numericKeys,
            string[]? optionalNumericKeys = null)
        {
            var missingKpis = new List<string>();
            var invalidNumericKpis = new List<string>(); // Added to track numeric format failures

            // 1. Process Mandatory Keys
            foreach (var key in dateKeys.Concat(numericKeys))
            {
                string fullKpiPath = $"{key}";

                if (!section.TryGetValue(key, out var kpiData) || kpiData?.Value == null)
                {
                    missingKpis.Add(fullKpiPath);
                    continue;
                }

                string stringValue = kpiData.Value.ToString() ?? string.Empty;

                if (dateKeys.Contains(key))
                {
                    ValidateDate(context, elementId, elementPath, fullKpiPath, stringValue);
                }
                else if (numericKeys.Contains(key))
                {
                    // Instead of firing an error immediately inside ValidateNumeric, 
                    // check format directly or capture it if it fails:
                    if (!double.TryParse(stringValue, out double val) || val < 0)
                    {
                        invalidNumericKpis.Add(fullKpiPath);
                    }
                }
            }

            // 2. Process Optional Numeric Keys
            if (optionalNumericKeys != null)
            {
                foreach (var key in optionalNumericKeys)
                {
                    string fullKpiPath = $"{key}";

                    if (section.TryGetValue(key, out var kpiData) && kpiData?.Value != null)
                    {
                        string stringValue = kpiData.Value.ToString() ?? string.Empty;
                        if (!double.TryParse(stringValue, out double val) || val < 0)
                        {
                            invalidNumericKpis.Add(fullKpiPath);
                        }
                    }
                }
            }

            if (missingKpis.Any())
            {
                string missingList = string.Join(", ", missingKpis.Select(k => $"'{k}'"));
                context.AddFailure(new ValidationFailure(elementPath, $"Fields {missingList} are missing from the '{sectionName}' section.")
                {
                    ErrorCode = "MISSING_MANDATORY_CARBON_KPI",
                    CustomState = new { elementId = (string)null, kpis = missingKpis }
                });
            }

            // Grouped Invalid Numeric Fields Error
            if (invalidNumericKpis.Any())
            {
                string invalidList = string.Join(", ", invalidNumericKpis.Select(k => $"'{k}'"));
                context.AddFailure(new ValidationFailure(elementPath, $"Values for fields {invalidList} must be valid positive numbers.")
                {
                    ErrorCode = "INVALID_NUMERIC_VALUE",
                    CustomState = new { elementId = (string)null, kpis = invalidNumericKpis }
                });
            }
        }



        private static void ValidateDate(ValidationContext<KpiSubmissionRequestV2> context, string elementId, string elementPath, string kpiKey, string value)
        {
            bool isValidDate = DateTime.TryParseExact(value, ["yyyy-MM-dd"],
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);

            if (!isValidDate)
            {
                context.AddFailure(new ValidationFailure(elementPath, $"Value for '{kpiKey}' must be a valid date in YYYY-MM-DD format.")
                {
                    ErrorCode = "INVALID_DATE_FORMAT",
                    CustomState = new { elementId = (string)null, kpis = new List<string> { kpiKey } }
                });
            }
        }
    }
}

