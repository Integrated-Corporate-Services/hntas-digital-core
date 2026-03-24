namespace HNTAS.Core.Api.Models
{
    public class AuditLogResponse
    {
        public string EntryType { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string Timestamp { get; set; }
        public string? ElementName { get; set; }
        public string? Phase { get; set; }
        public string? Stage { get; set; }
    }
}
