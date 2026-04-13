using HNTAS.Core.Api.Common;

namespace HNTAS.Core.Api.Validators.Arms
{
    public interface IHeatNetworkValidator
    {
        Task<ValidationGateResult> ValidateAsync(string networkId, IEnumerable<string> elementIds);
    }
}
