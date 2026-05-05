namespace HNTAS.Core.Api.Models.Arms.Dashboard
{
    public class KpiHistoryResponse
    {
        public DateTime Timestamp { get; set; }
        public string SourceSystem { get; set; }
        public string KpiId { get; set; }
        public string ElementId { get; set; }
        public bool IsAggregated { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public bool IsImputed { get; set; }
    }
}
