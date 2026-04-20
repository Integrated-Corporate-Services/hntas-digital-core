namespace HNTAS.Core.Api.Models.Soa
{
    public class ElementSoaAssignAssessorRequest
    {
        public List<string> ElementIds { get; set; } = [];
        public string AssessorEmail { get; set; } = string.Empty;
        public string AssessorFirstName { get; set; } = string.Empty;
        public string AssessorLastName { get; set; } = string.Empty;
        public string Assessment { get; set; } = string.Empty;
        public string HnId { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
