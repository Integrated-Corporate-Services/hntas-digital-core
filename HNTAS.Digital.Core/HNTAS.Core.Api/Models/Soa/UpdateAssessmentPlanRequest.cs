using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class UpdateAssessmentPlanRequest
    {
        [Required]
        public string HnId { get; set; } = string.Empty;

        [Required]
        public SoaPhase Phase { get; set; }

        public SoaStage? Stage { get; set; }

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string S3Key { get; set; } = string.Empty;
    }
}
