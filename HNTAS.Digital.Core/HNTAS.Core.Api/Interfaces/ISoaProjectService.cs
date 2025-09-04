using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ISoaProjectService
    {
        Task<SoaProject> GetByIdAsync(string id);
        Task<SoaProject> GetByHeatNetworkIdAsync(string hnId);
        Task<SoaProject> CreateAsync(string hnId, string createdBy);
        Task UpdateNetworkTypeAsync(string hnId, string updatedBy, NetworkTypeSelection networkTypeSelection);
        Task UpdateConnectionTypesAsync(string hnId, string updatedBy, List<ConnectionType> connectionTypes);
        Task UpdateHeatNetworkElementsAsync(string hnId, List<HeatNetworkElement> elements, string updatedBy);
        Task UpdateElementLocationsAsync(string projectId, HeatNetworkElementType elementType, List<string> locations, string updatedBy);
        Task UpdateElementDocumentsAsync(string hnId, HeatNetworkElementType elementType, List<UploadedDocument> documents, string updatedBy);
    }
}
