using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class UserRoleDetailResponse
    {
        public string FullName { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string EmailId { get; set; } = null!;
        public string RoleDescription { get; set; } = null!;
    }
}
