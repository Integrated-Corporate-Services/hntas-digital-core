using HNTAS.Core.Api.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class ElementSoaAssignAssessorRequest
    {
        public List<AssessorAssessmentForElement> AssessorAssessmentForElements { get; set; } = [];
        public SoaStage SoaStage { get; set; }
        public string HnId { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    public class AssessorAssessmentForElement
    {
        public ElementTypeInShort ElementType { get; set; }
        public List<AssessorAssessment> AssessorAssessments { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class AssessorAssessment
    {
        public string AssessorEmail { get; set; } = string.Empty;
        public string AssessorFirstName { get; set; } = string.Empty;
        public string AssessorLastName { get; set; } = string.Empty;
        public string Assessment { get; set; } = string.Empty;
    }
}
