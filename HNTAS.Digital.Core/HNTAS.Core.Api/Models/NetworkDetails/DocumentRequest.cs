using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.NetworkDetails
{
    [ExcludeFromCodeCoverage]
    public class DocumentRequest
    {
        [Required]
        public string HnId { get; set; } = string.Empty;        

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
