using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateConnectionsRequest
    {
        [Required(ErrorMessage = "HnId is required.")]
        public string HnId { get; set; } = null!;

        [Required(ErrorMessage = "Connection types are required.")]
        [MinLength(1, ErrorMessage = "At least one connection type must be selected.")]
        public List<ConnectionType> ConnectionTypes { get; set; }
    }
}
