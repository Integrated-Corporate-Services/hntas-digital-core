using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ICarbonCalculatorService
    {
        Task<CarbonCalculatorResponse?> RunAsync(CarbonCalculatorRequest hnId, CancellationToken ct = default);
    }
}
