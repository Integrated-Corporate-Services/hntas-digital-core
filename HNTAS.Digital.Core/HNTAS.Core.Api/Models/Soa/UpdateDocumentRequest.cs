using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class UpdateDocumentRequest : DocumentRequest
    {
        [Required]
        public SoaPhase Phase { get; set; }

        public SoaStage? Stage { get; set; }
    }
}
