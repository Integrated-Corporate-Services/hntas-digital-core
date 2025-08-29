using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ISoaProjectService
    {
        Task<SoaProject> GetByIdAsync(string id);
        Task<SoaProject> GetByHeatNetworkIdAsync(string hnId);
        Task<SoaProject> CreateAsync(string hnId);
        Task UpdateNetworkTypeAsync(string hnId, NetworkTypeSelection networkTypeSelection);
        Task UpdateConnectionTypesAsync(string hnId, List<ConnectionType> connectionTypes);
    }
}
