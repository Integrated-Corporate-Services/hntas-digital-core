namespace HNTAS.Core.Api.Models.Users
{
    public class ManagedUserResponse
    {
        public UserResponse ResponsibleUser { get; set; }
        public List<UserResponse> RegisteredUsers { get; set; } = [];
        public List<InvitedUserResponse> InvitedUsers { get; set; } = [];
    }
}
