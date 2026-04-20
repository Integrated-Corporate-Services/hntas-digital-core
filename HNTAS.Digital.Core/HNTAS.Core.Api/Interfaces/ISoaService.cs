using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ISoaService
    {
        Task<Soa> GetByHeatNetworkIdAsync(string hnId);
        Task<Soa> CreateAsync(string hnId, string createdBy);
        Task<Soa?> UpdateStatusAsync(string hnId, SoaStatus newStatus, string updatedBy);
        Task DeleteByHeatNetworkIdAsync(string hnId);
        Task UpdateNetworkTypeAsync(string hnId, string updatedBy, NetworkTypeSelection networkTypeSelection);
        Task UpdateConnectionTypesAsync(string hnId, string updatedBy, List<ConnectionType> connectionTypes);
        Task UpdateHeatNetworkElementsAsync(string hnId, List<HeatNetworkElement> elements, string updatedBy);
        Task UpdateElementLocationsAsync(string projectId, HeatNetworkElementType elementType, List<string> locations, string updatedBy);
        Task UpdateElementDocumentsAsync(string hnId, HeatNetworkElementType elementType, List<UploadedDocument> documents, string updatedBy);
        Task UpdateAssessmentDocumentAsync(string hnId, Document document);
        Task UpdateAssessorDocumentAsync(string hnId, Document document);
        Task UpdateCertifierDocumentAsync(string hnId, Document document);
        Task UpdateSoaStatus(string hnId, string elementId, SoaStage stage, string soaStatus, string updatedBy, NetworkDetailsStatus elementSoaStatus);
    }
}
