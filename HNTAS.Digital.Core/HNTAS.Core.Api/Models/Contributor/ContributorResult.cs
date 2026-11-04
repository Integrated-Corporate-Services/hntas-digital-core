namespace HNTAS.Core.Api.Models.Contributor
{
    public class ContributorResult
    {
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string OrganisationId { get; set; } = null!;
        public string? WorkingOrgName { get; set; }
    }
}
