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

            // TODO: Introduce a new mapping
            // Safely handle cases where Elements might be null
            //var registeredElements = network.NetworkElements?.Elements?
            //    .ToDictionary(e => e.ElementId, e => e.Type.ToString())
            //    ?? new Dictionary<string, string>();
            var registeredElements = network.NetworkElements?.ElementsGroup?
                .ToDictionary(e => e.ElementType, e => e.ElementDisplayType.ToString())
                ?? new Dictionary<string, string>();

            var apiErrors = new List<KpiSubmissionApiError>();

            foreach (var element in elements)
            {
                // 1. Check if ID exists
                if (!registeredElements.TryGetValue(element.ElementId, out var registeredType))
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "ELEMENT_NOT_FOUND",
                        Message = "The provided Element ID is not associated with this network.",
                        ElementId = element.ElementId
                    });
                    continue;
                }

                // 2. Check if Type matches
                if (registeredType != element.Type.ToString())
                {
                    apiErrors.Add(new KpiSubmissionApiError
                    {
                        Code = "ELEMENT_TYPE_MISMATCH",
                        Message = $"Element type mismatch. Expected '{registeredType}', but received '{element.Type}'.",
                        ElementId = element.ElementId
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
