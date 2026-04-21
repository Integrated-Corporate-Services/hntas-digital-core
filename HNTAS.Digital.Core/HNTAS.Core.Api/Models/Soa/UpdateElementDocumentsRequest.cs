using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Models.Soa
{
    public class UpdateElementDocumentsRequest
    {
        public string HnId { get; set; } = null!;
        public HeatNetworkElementType ElementType { get; set; }
        public string UpdatedBy { get; set; } = null!;
        public List<UploadedDocument> Documents { get; set; } = [];
    }
}
