namespace HNTAS.Core.Api.Models
{
    public class UserRoleDetailResponse
    {
        public string FullName { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string EmailId { get; set; } = null!;
        public string RoleDescription { get; set; } = null!;
    }
}
