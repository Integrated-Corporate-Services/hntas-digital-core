using HNTAS.Core.Api.Enums;

namespace HNTAS.Core.Api.Models.Soa
{
    public class HeatNetworkResponse
    {
        public string Id { get; set; } = null!;
        public string HnId { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Pathway { get; set; } = null!;
        public SoaResponse? Soa { get; set; }
    }

    public class SoaResponse
    {
        public SoaStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public JourneyDataResponse? JourneyData { get; set; }
    }

    public class JourneyDataResponse
    {
        public NetworkTypeResponse? NetworkType { get; set; }
        public List<string>? ConnectionTypes { get; set; }
        public List<HeatNetworkElementResponse> HeatNetworkElements { get; set; } = new();
        public List<AssessmentPlanDocumentResponse> AssessmentPlans { get; set; } = new();
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


}
