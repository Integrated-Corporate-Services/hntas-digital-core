using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAsync();
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUserOneLoginIdAsync(string userId);
        Task CreateAsync(User newUser);
        Task UpdateAsync(string id, User updatedUser);
        Task RemoveAsync(string id);
        Task<List<User>> GetRegisteredUsers(List<string> invitedEmails);
        Task<UserDetailsResponse> GetUserWithDetailsAsync(string userId);

        Task<List<User>> GetAssessorsByHnIdAsync(string hnId);
        Task<User?> GetResponsiblePersonByHnIdAsync(string hnId);
        Task<List<User>> GetContributorsByHnIdAsync(string hnId);

        Task<List<ManagedUserResponse>> GetRegisteredUsersDetailsAsync(List<string> invitedEmails);
    }
}
