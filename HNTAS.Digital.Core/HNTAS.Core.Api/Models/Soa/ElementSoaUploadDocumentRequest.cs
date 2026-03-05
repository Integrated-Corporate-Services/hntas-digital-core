using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;

namespace HNTAS.Core.Api.Models.Soa
{
    public class ElementSoaUploadDocumentRequest : DocumentRequest
    {
        public SoaStage Stage { get; set; }
        public string? ElementId { get; set; }

        public NetworkDetailsStatus ElementSoaStatus { get; set; }
    }
}
