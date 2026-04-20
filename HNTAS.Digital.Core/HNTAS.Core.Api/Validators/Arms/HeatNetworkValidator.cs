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
                return new ValidationGateResult(false, $"Network ID '{networkId}' is not registered.", 404);

            // Create a dictionary for quick lookup: Key = ID, Value = Type
            var registeredElements = network.NetworkElements?.Elements
                .ToDictionary(e => e.ElementId, e => e.Type.ToString());

            var invalidElements = new List<string>();

            foreach (var element in elements)
            {
                // Check if ID exists
                if (!registeredElements.TryGetValue(element.ElementId, out var registeredType))
                {
                    invalidElements.Add($"{element.ElementId} (Not found)");
                    continue;
                }

                // Check if Type matches
                if (registeredType != element.Type.ToString())
                {
                    invalidElements.Add($"{element.ElementId} (Expected {registeredType}, found {element.Type.ToString()})");
                }
            }

            if (invalidElements.Any())
                return new ValidationGateResult(false, $"Validation errors: {string.Join(", ", invalidElements)}.");

            return new ValidationGateResult(true);
        }
    }
}
