using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Services;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IEmailService
    {
        Task TrySendOrgCreatedEmailAsync(User user, Organisation organisation);
        Task TrySendOrgUpdatedEmailAsync(string fullName, string userEmail, string oldNameAndAddress, string newNameAndAddress);
        Task TrySendHeatNetworkRegistrationEmailAsync(string userEmail, string fullName, string hnId, string hnName);
        Task TrySendHeatNetworkInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName);
        Task TrySendOrganisationInvitationEmailAsync(Invitation invitation, string token, string organisationName, string inviterName);
        Task TrySendAssessorEmailAsync(string emailAddress, string hnName, string hnId, string contributorName);
        Task TrySendAssessorAssessmentEmailAsync(string emailAddress, string hnName, string hnId, string assessmentResult);
        Task TrySendCertificationCompleteEmailAsync(string emailAddress, string hnName, string hnId);
        Task TrySendHNDiscontinedEmailAsync(User userToUpdate, string name, ContributorRole contributorRole);
        Task TrySendOfgemDataForExistingOrgOrRpEmailAsync(OfgemDataModelForNotification ofgemData);
        Task TrySendOfgemDataForNewRpEmailAsync(OfgemDataModelForNotification ofgemData);
    }
}
