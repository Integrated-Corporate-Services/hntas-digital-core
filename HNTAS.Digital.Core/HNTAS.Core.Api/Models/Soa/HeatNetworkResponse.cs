using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Soa
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkResponse
    {
        public string Id { get; set; } = null!;
        public string UHnId { get; set; } = null!;
        public string HnId { get; set; } = null!;
        public string OrgId { get; set; } = null!;
        public ECDetails? ECDetails { get; set; } = null!;
        public RegisteredAddress? Address { get; set; }
        public string Name { get; set; } = null!;
        public string? AdditionalDescription { get; set; }
        public string? Pathway { get; set; } = null!;
        public RegistrationSource RegistrationSource { get; set; }
        public SoaResponse? Soa { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? Phase { get; set; } = null;
        public HeatNetworkType? HeatNetworkType { get; set; }
        public bool HasOwnEnergyCentre { get; set; }
        public HeatNetworkConnections? HeatNetworkConnections { get; set; } = new();
        public NetworkElementsResponse? NetworkElements { get; set; }
        public MeteringAndMonitoringStrategyResponse? MeteringAndMonitoringStrategy { get; set; }
        public AssessmentPlanResponse? AssessmentPlan { get; set; }
        public DesignConstructionLogResponse? DesignConstructionLog { get; set; }
        public DateTime? OfgemImportedDate { get; set; }

    }

    [ExcludeFromCodeCoverage]
    public class SoaResponse : NetworkDetailsResponseBase
    {
        public SoaStatus Status { get; set; }
        public JourneyDataResponse? JourneyData { get; set; }
    }    

    [ExcludeFromCodeCoverage]
    public class JourneyDataResponse
    {
        public NetworkTypeResponse? NetworkType { get; set; }
        public List<string>? ConnectionTypes { get; set; }
        public List<HeatNetworkElementResponse> HeatNetworkElements { get; set; } = new();
        public List<UploadedAssessmentDocumentResponse> AssessmentDocs { get; set; } = new();
        public List<UploadedAssessorDocumentResponse> AssessorDocs { get; set; } = new();
        public List<UploadedCertifierDocumentResponse> CertifierDocs { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadedAssessmentDocumentResponse : UploadedDocumentResponse
    {
    }

    [ExcludeFromCodeCoverage]
    public class UploadedAssessorDocumentResponse : UploadedDocumentResponse
    {
    }

    [ExcludeFromCodeCoverage]
    public class UploadedCertifierDocumentResponse : UploadedDocumentResponse
    {
    }

    [ExcludeFromCodeCoverage]
    public class NetworkTypeResponse
    {
        public string Type { get; set; } = null!;
        public string? OtherNetworkDescription { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class HeatNetworkElementResponse
    {
        public string Name { get; set; } = null!;
        public int Count { get; set; }
        public List<string> Locations { get; set; } = new();
        public List<UploadedDocumentResponse> Documents { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class UploadedDocumentResponse
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string? Stage { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class UploadedDocumentResponse2
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string Stage { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class AssessmentPlanDocumentResponse
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string? Stage { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    [ExcludeFromCodeCoverage]
    public class NetworkElementsResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus NetworkElementStatus { get; set; }
        public NetworkDetailsStatus ElementSoaStatus { get; set; }
        //public string? ElementType { get; set; }
        public List<ElementGroup> ElementsGroup { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class MeteringAndMonitoringStrategyResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class AssessmentPlanResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class DesignConstructionLogResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
        public List<NetworkDetailsUploadedDocument> Documents { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class NetworkDetailsResponseBase
    {        
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }        
    }
}
