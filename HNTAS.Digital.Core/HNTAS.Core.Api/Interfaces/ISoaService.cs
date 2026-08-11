using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.Soa;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ISoaService
    {
        Task<Soa?> UpdateStatusAsync(string hnId, SoaStatus newStatus, string updatedBy);        
        Task UpdateSoaStatus(string hnId, ElementTypeInShort elementType, SoaStage stage, List<SoaStatusWithCount> soaStatuses, string updatedBy, NetworkDetailsStatus elementSoaStatus);
        Task<NetworkElements> UpdateAssignAssessor(ElementSoaAssignAssessorRequest request, NetworkElements networkElements, string phase, bool initiateSoa);
        Task<NetworkElements> UpdateAssignAssessorForExistingNetwork(ElementSoaAssignAssessorRequestForExistingNetwork request, NetworkElements networkElements, string phase, bool initiateSoa);
        Task UpdateSoaStatusForExistingNetwork(string hnId, ElementTypeInShort elementType, Milestone milestone, List<SoaStatusWithCountExistingNetwork> soaStatuses, string updatedBy, NetworkDetailsStatus elementSoaStatus);
    }
}
