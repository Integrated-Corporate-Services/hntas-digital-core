using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IEmailService
    {
        Task TrySendOrgCreatedEmailAsync(User user, Organisation organisation);
        Task TrySendOrgUpdatedEmailAsync(string fullName, string userEmail, string oldNameAndAddress, string newNameAndAddress);
        Task TrySendInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName);
        Task TrySendAssessorEmailAsync(string emailAddress, string hnName, string hnId, string contributorName);
        Task TrySendAssessorAssessmentEmailAsync(string emailAddress, string hnName, string hnId, string assessmentResult);
        Task TrySendCertificationCompleteEmailAsync(string emailAddress, string hnName, string hnId);
        Task TrySendHNDiscontinedEmailAsync(User userToUpdate, string name, ContributorRole contributorRole);
    }
}
