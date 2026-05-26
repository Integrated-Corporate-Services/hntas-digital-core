using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class HeatNetworkValidator : IHeatNetworkValidator
    {
        private readonly IHeatNetworkService _heatNetworkService;

        public HeatNetworkValidator(IHeatNetworkService heatNetworkService)
        {
            _heatNetworkService = heatNetworkService;
        }

        // Assuming your element object looks like this
        public async Task<ValidationGateResult> ValidateAsync(string networkId, IEnumerable<NetworkElementRequest> elements)
        {
            var network = await _heatNetworkService.GetByHnIdAsync(networkId);

            if (network == null)
            {
                return new ValidationGateResult(
                     IsValid: false,
                     Message: "Validation Failed",
                     Detail: $"Network ID '{networkId}' is not registered.",
                     StatusCode: 404,
                     Errors: new List<KpiSubmissionApiError>
                     {
                        new KpiSubmissionApiError
                        {
                            Code = "NETWORK_NOT_REGISTERED",
                            Message = $"Network ID '{networkId}' is not registered.",
                            ElementId = null
                        }
                     }
                 );
            }

            var registeredElements = network.NetworkElements?.ElementsGroup?
              .Where(e => e.ElementDisplayType != null)
              .ToDictionary(e => e.ElementDisplayType.ToString(), e => e.Count)
              ?? new Dictionary<string, int?>();

            var apiErrors = new List<KpiSubmissionApiError>();

            // 1. Check for any duplicate ElementIds across the entire payload first
            var duplicateElementIds = elements
                .Where(e => !string.IsNullOrWhiteSpace(e.ElementId))
                .GroupBy(e => e.ElementId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateElementIds.Any())
            {
                foreach (var dupId in duplicateElementIds)
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "DUPLICATE_ELEMENT_ID",
                        Message = $"The submission contains duplicate element ID '{dupId}'. Each element must have a unique identifier.",
                        ElementId = null
                    });
                }
            }

            // Group incoming elements by Type upfront
            // CRITICAL FIX: Use Distinct().Count() for calculations so duplicates don't mask a mismatch
            var incomingElementCounts = elements
                .Where(e => !string.IsNullOrWhiteSpace(e.Type))
                .GroupBy(e => e.Type.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ElementId).Distinct().Count()
                );

            // 2. NEW FIX: Find any types submitted by the user that DO NOT exist in the database registry at all
            var nonExistentTypes = incomingElementCounts.Keys
                .Where(type => !registeredElements.ContainsKey(type))
                .ToList();

            if (nonExistentTypes.Any())
            {
                foreach (var invalidType in nonExistentTypes)
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "ELEMENT_TYPE_NOT_FOUND",
                        Message = $"Element type '{invalidType}' is invalid or does not exist for this heat network.",
                        ElementId = null
                    });
                }
            }

            // 3. Loop through the valid registered requirements to check for exact volume matches
            foreach (var registeredType in registeredElements.Keys)
            {
                var registeredCount = registeredElements[registeredType] ?? 0;

                // Get the count received (if the type was completely missing from the payload, it defaults to 0)
                incomingElementCounts.TryGetValue(registeredType, out var receivedCount);

                // Check if the received count does not match the expected registry totals exactly
                if (receivedCount != registeredCount)
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "ELEMENT_COUNT_NOT_MATCHED",
                        Message = $"Element count mismatch for type '{registeredType}'. Expected '{registeredCount}', but received '{receivedCount}'.",
                        ElementId = null
                    });
                }
            }

            if (apiErrors.Any())
            {
                return new ValidationGateResult(
                    IsValid: false,
                    Message: "Validation Failed",
                    Detail: "One or more elements do not match the heat network registry.",
                    StatusCode: 400,
                    Errors: apiErrors
                );
            }

            return new ValidationGateResult(true);
        }
    }
}
