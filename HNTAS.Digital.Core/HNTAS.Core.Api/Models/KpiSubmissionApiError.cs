namespace HNTAS.Core.Api.Models
{
    public class KpiSubmissionApiError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string? ElementId { get; set; }
        public List<string>? Kpis { get; set; }
    }
}
