using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Interfaces;
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
                return new ValidationGateResult(false, $"Network ID '{networkId}' is not registered.", 404);
            }

            // Safely handle cases where Elements might be null
            var registeredElements = network.NetworkElements?.Elements?
                .ToDictionary(e => e.ElementId, e => e.Type.ToString())
                ?? new Dictionary<string, string>();

            var invalidElements = new List<string>();

            foreach (var element in elements)
            {
                // 1. Check if ID exists
                if (!registeredElements.TryGetValue(element.ElementId, out var registeredType))
                {
                    invalidElements.Add($"Element ID '{element.ElementId}' not found.");
                    continue;
                }

                // 2. Check if Type matches
                if (registeredType != element.Type.ToString())
                {
                    invalidElements.Add($"Element ID '{element.ElementId}' type mismatch: Expected '{registeredType}', but found '{element.Type}'.");
                }
            }

            // Return the list of errors separately from the summary message
            if (invalidElements.Any())
            {
                return new ValidationGateResult(
                    IsValid: false,
                    Message: "Registry mismatch detected.", // High-level summary
                    StatusCode: 400,
                    Errors: invalidElements // The specific list of mismatches
                );
            }

            return new ValidationGateResult(true);
        }
    }
}
