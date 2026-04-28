using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IInvitationService
    {
        Task<List<Invitation>> GetAsync();
        Task<Invitation> GetByIdAsync(string id);
        Task<Invitation> GetByEmailAsync(string invitedEmail, string hnId);
        Task<List<Invitation>> GetByInvitedUserIdAsync(string inviterUserId);
        Task CreateAsync(Invitation newInvitation);
        Task UpdateAsync(string id, Invitation updatedInvitation);
        Task RemoveAsync(string id);

        Task<List<ManagedUserResponse>> GetInvitedUsersAsRegisteredAsync(string inviterUserId);

        Task ExecuteRoleSwapAsync(User invitedUser, User? replacedUser, Invitation invitation);
        Task<Invitation> GetByInvitedDetailsAsync(string invitedEmailId, string invitedHnId, ContributorRole invitedRole);
        Task<List<Invitation>> GetByEmailsAndHnIdAsync(List<string> invitedEmails, string hnId);
    }
}
