using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IHeatNetworkService
    {
        Task<List<HeatNetwork>> GetAsync();
        Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> ids);
        Task<HeatNetwork> GetByHnIdAsync(string hnId);
        Task CreateAsync(HeatNetwork newHeatNetwork);
        Task UpdateAsync(string id, HeatNetwork updatedHn);
        Task<List<HeatNetwork>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);


        // --- Enriched Detail Methods (External/Response DTOs) ---
        Task<HeatNetworkExternalResponse> GetDetailsByHnIdAsync(string hnId);
        Task<List<HeatNetworkExternalResponse>> GetDetailsAsync();
        Task<List<HeatNetworkExternalResponse>> GetDetailsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }
}
