using FluentValidation;
using FluentValidation.Results;
using HNTAS.Core.Api.Models.Arms.V2;
using ElementType = HNTAS.Core.Api.Enums.HeatNetworkElementType;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class KpiCarbonInputsValidator : AbstractValidator<KpiSubmissionRequestV2>
    {
        public KpiCarbonInputsValidator()
        {
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
