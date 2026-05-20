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
                            Message = $"Network ID '{networkId}' is not registered."
                        }
                     }
                 );
            }

            var registeredElements = network.NetworkElements?.ElementsGroup?
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
                        Message = $"The submission contains duplicate element ID '{dupId}'. Each element must have a unique identifier."
                    });
                }
            }

            // Group incoming elements by Type upfront to avoid redundant scans and duplicate errors
            var incomingElementCounts = elements
                .GroupBy(e => e.Type.ToString())
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), FirstElementId = g.First().ElementId });

            // Loop through the registered requirements to check for matches
            foreach (var registeredType in registeredElements.Keys)
            {
                var registeredCount = registeredElements[registeredType] ?? 0;

                // Get the actual count received (default to 0 if none of this type were provided)
                incomingElementCounts.TryGetValue(registeredType, out var incomingData);
                int receivedCount = incomingData?.Count ?? 0;

                // Check if the received count does not match the registry exactly
                if (receivedCount != registeredCount)
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "ELEMENT_COUNT_NOT_MATCHED",
                        Message = $"Element count mismatch for type '{registeredType}'. Expected '{registeredCount}', but received '{receivedCount}'.",
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
