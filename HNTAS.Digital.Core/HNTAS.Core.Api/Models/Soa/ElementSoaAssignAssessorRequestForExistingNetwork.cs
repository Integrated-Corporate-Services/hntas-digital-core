using HNTAS.Core.Api.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class ElementSoaAssignAssessorRequestForExistingNetwork
    {
        public List<AssessorAssessmentForElement> AssessorAssessmentForElements { get; set; } = [];
        public Milestone Milestone { get; set; }
        public string HnId { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }    
}
