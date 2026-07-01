using HNTAS.Core.Api.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class UpdateSoaStatusRequest
    {
        public string HnId { get; set; } = null!;
        public string HnName { get; set; } = null!;
        public string UpdatedBy { get; set; } = null!;
        public SoaStatus Status { get; set; }
    }
}
