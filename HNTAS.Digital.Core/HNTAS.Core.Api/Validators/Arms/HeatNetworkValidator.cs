using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Interfaces;

namespace HNTAS.Core.Api.Validators.Arms
{
    public class HeatNetworkValidator : IHeatNetworkValidator
    {
        private readonly IHeatNetworkService _heatNetworkService;

        public HeatNetworkValidator(IHeatNetworkService heatNetworkService)
        {
            _heatNetworkService = heatNetworkService;
        }

        public async Task<ValidationGateResult> ValidateAsync(string networkId, IEnumerable<string> elementIds)
        {
            var network = await _heatNetworkService.GetByHnIdAsync(networkId);

            // Check if Network exists
            if (network == null)
                return new ValidationGateResult(false, $"Network ID '{networkId}' is not registered.", 404);

            // Check if all provided ElementIds belong to this Network
            var registeredIds = network.NetworkElements?.Elements.Select(e => e.ElementId).ToHashSet();
            if (registeredIds == null)
                return new ValidationGateResult(false, $"Network ID '{networkId}' has no registered elements.");

            var unknownIds = elementIds.Where(id => !registeredIds.Contains(id)).ToList();

            if (unknownIds.Any())
                return new ValidationGateResult(false, $"Unknown Element IDs: {string.Join(", ", unknownIds)}.");

            return new ValidationGateResult(true);
        }
    }
}
