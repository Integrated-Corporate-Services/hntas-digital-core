using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models.Soa
{
    public class HeatNetworkResponse
    {
        public string Id { get; set; } = null!;
        public string HnId { get; set; } = null!;
        public ECDetails ECDetails { get; set; } = null!;
        public RegisteredAddress? Address { get; set; }
        public string Name { get; set; } = null!;
        public string Pathway { get; set; } = null!;
        public SoaResponse? Soa { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? Phase { get; set; } = null;
        public NetworkCharacteristicsResponse? NetworkCharacteristics { get; set; }
        public NetworkElementsResponse? NetworkElements { get; set; }
        public MeteringAndMonitoringStrategyResponse? MeteringAndMonitoringStrategy { get; set; }
        public AssessmentPlanResponse? AssessmentPlan { get; set; }
        public DesignConstructionLogResponse? DesignConstructionLog { get; set; }

    }

    public class SoaResponse : NetworkDetailsResponseBase
    {
        public SoaStatus Status { get; set; }
        public JourneyDataResponse? JourneyData { get; set; }
    }

    public class JourneyDataResponse
    {
        public NetworkTypeResponse? NetworkType { get; set; }
        public List<string>? ConnectionTypes { get; set; }
        public List<HeatNetworkElementResponse> HeatNetworkElements { get; set; } = new();
        public List<UploadedAssessmentDocumentResponse> AssessmentDocs { get; set; } = new();
        public List<UploadedAssessorDocumentResponse> AssessorDocs { get; set; } = new();
        public List<UploadedCertifierDocumentResponse> CertifierDocs { get; set; } = new();
    }

    public class UploadedAssessmentDocumentResponse : UploadedDocumentResponse
    {
    }

    public class UploadedAssessorDocumentResponse : UploadedDocumentResponse
    {
    }

    public class UploadedCertifierDocumentResponse : UploadedDocumentResponse
    {
    }

    public class NetworkTypeResponse
    {
        public string Type { get; set; } = null!;
        public string? OtherNetworkDescription { get; set; }
    }

    public class HeatNetworkElementResponse
    {
        public string Name { get; set; } = null!;
        public int Count { get; set; }
        public List<string> Locations { get; set; } = new();
        public List<UploadedDocumentResponse> Documents { get; set; } = new();
    }
    public class UploadedDocumentResponse
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string? Stage { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    public class UploadedDocumentResponse2
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string Stage { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }
    public class AssessmentPlanDocumentResponse
    {
        public string FileName { get; set; } = null!;
        public string S3Key { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string? Stage { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; } = null!;
    }

    public class NetworkCharacteristicsResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }        
        public string? Id { get; set; }       
        public HeatNetworkType HeatNetworkType { get; set; }        
        public string? HeatGenerationSourceFor { get; set; }
        public int? NumberOfCommunalFloors { get; set; }

        public bool? ContainsPressureBreak { get; set; }

        public bool IsSupplyingOtherHeatNetworks { get; set; }

        public bool HasCommercialConnections { get; set; }

        public bool IsSuppliedByADistrictHeatNetwork { get; set; }
    }
    public class NetworkElementsResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
    }
    public class MeteringAndMonitoringStrategyResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
    }
    public class AssessmentPlanResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
    }
    public class DesignConstructionLogResponse : NetworkDetailsResponseBase {
        public NetworkDetailsStatus Status { get; set; }
    }

    public class NetworkDetailsResponseBase
    {        
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }        
    }
}
