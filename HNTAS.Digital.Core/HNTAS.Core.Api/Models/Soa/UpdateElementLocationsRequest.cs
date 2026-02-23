using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateElementLocationsRequest
    {
        [Required(ErrorMessage = "HnId is required.")]
        public string HnId { get; set; } = null!;

        [Required(ErrorMessage = "UpdatedBy is required.")]
        public string UpdatedBy { get; set; } = null!;

        [Required(ErrorMessage = "Element type is required.")]
        [EnumDataType(typeof(HeatNetworkElementDisplayType), ErrorMessage = "Invalid element type.")]
        public HeatNetworkElementDisplayType ElementType { get; set; }

        [Required(ErrorMessage = "At least one location must be provided.")]
        [MinLength(1, ErrorMessage = "At least one location must be provided.")]
        public List<string> Locations { get; set; } = new();
    }
}
