using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NetworkDetails;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class ElementSoaUploadDocumentRequest : DocumentRequest
    {
        public SoaStage Stage { get; set; }
        public string? ElementId { get; set; }

        public NetworkDetailsStatus ElementSoaStatus { get; set; }
    }
}
