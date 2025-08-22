namespace HNTAS.Core.Api.Models.Users
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string OneLoginId { get; set; }
        public string EmailId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string JobTitle { get; set; }
        public string? PreferredContactType { get; set; }
        public string? LandlineNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? OrgId { get; set; }
        public List<string>? Roles { get; set; }
        public string Status { get; set; }

        public List<string>? HnIds { get; set; }

    }
}
