using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Soa
{
    public class ElementSoaStatusUpdateRequest
    {
        public SoaStage Stage { get; set; }
        public string? ElementId { get; set; }
        public string? SoaStatus { get; set; }
        public NetworkDetailsStatus ElementSoaStatus { get; set; }
        public string? SoaStatusUpdatedBy { get; set; }
        [Required]
        public string HnId { get; set; } = string.Empty;
    }
}
