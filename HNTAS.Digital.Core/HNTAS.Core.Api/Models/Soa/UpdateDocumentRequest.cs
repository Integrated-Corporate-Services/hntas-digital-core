using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateDocumentRequest : DocumentRequest
    {
        [Required]
        public SoaPhase Phase { get; set; }

        public SoaStage? Stage { get; set; }
    }
}
