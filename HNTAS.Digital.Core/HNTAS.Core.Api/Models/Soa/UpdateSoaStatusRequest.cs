using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateSoaStatusRequest
    {
        public string HnId { get; set; } = null!;
        public string UpdatedBy { get; set; } = null!;
        public SoaStatus Status { get; set; }
    }
}
