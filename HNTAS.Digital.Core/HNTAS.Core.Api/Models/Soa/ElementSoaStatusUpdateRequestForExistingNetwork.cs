using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class ElementSoaStatusUpdateRequestForExistingNetwork
    {
        public Milestone Milestone { get; set; }
        public string? ElementId { get; set; }
        public ElementTypeInShort ElementType { get; set; }
        public List<SoaStatusWithCountExistingNetwork>? SoaStatuses { get; set; }
        public NetworkDetailsStatus ElementSoaStatus { get; set; }
        public string? SoaStatusUpdatedBy { get; set; }
        [Required]
        public string HnId { get; set; } = string.Empty;
        public string? SoaPhase { get; set; }
        public string? ElementDisplayName { get; set; }
    }
}
