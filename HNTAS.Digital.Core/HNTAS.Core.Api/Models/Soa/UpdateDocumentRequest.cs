using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateDocumentRequest
    {
        [Required]
        public string HnId { get; set; } = string.Empty;

        [Required]
        public SoaPhase Phase { get; set; }

        public SoaStage? Stage { get; set; }

        [Required]
        public string UploadedBy { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string S3Key { get; set; } = string.Empty;

        [Required]
        public DocumentType DocumentType { get; set; }
    }
}
