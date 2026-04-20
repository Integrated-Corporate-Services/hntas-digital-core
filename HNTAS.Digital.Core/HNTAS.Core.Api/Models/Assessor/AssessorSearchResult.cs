namespace HNTAS.Core.Api.Models.Assessor
{
    public class AssessorSearchResult
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullNameWithEmail { get; set; } = null!;
    }
}
