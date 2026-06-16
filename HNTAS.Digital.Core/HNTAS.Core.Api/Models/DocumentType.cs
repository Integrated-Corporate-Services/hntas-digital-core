using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentType
    {
        Assessment = 1,
        Assessor,
        Certifier,
        MeteringAndMonitoringStrategy,
        AssessmentPlan,
        DesignConstructionLog,
        Soa
    }
}
