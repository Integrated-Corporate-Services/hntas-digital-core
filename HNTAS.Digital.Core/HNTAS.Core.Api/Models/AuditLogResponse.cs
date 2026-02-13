namespace HNTAS.Core.Api.Models
{
    public class AuditLogResponse
    {
        public string Event { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string Timestamp { get; set; }
    }
}
