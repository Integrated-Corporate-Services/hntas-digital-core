using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Models
{
    public class UserDetailsResponse
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
        public string? ContactNumberExtension { get; set; }
        public string? MobileNumber { get; set; }
        public UserStatus Status { get; set; }
        public List<UserRole>? Roles { get; set; }
        public OrganisationResponse? Organisation { get; set; }
        public List<HeatNetworkUserResponse>? HeatNetworks { get; set; }

    }

    public class OrganisationResponse
    {
        public string OrgId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? CompaniesHouseNumber { get; set; }
        public OrganisationType Type { get; set; }
        public RegisteredAddress RegisteredAddress { get; set; }
    }
}
