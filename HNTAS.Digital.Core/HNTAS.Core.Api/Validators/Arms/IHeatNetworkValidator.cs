using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Models.Arms;

namespace HNTAS.Core.Api.Validators.Arms
{
    public interface IHeatNetworkValidator
    {
        Task<ValidationGateResult> ValidateAsync(string networkId, IEnumerable<NetworkElementRequest> elements);
    }
}
