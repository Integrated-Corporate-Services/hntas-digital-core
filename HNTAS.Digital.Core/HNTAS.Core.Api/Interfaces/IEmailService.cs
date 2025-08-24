using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IEmailService
    {
        Task TrySendOrgCreatedEmailAsync(User user, Organisation organisation);
        Task TrySendInvitationEmailAsync(Invitation invitation, string token, string heatNetworkName);
    }
}
