using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class UserResponse
    {
        public string Id { get; set; } = null!;
        public string OneLoginId { get; set; } = null!;
        public string EmailId { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public PreferredContactType? PreferredContactType { get; set; }
        public string? LandlineNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? ContactNumberExtension { get; set; }
        public string? OrgId { get; set; }
        public List<UserRole>? Roles { get; set; }
        public UserStatus Status { get; set; }
        public List<HnRoleMapping>? HnRoleMappings { get; set; }
        public List<string>? ContributingOrganisations { get; set; }
    }
}
