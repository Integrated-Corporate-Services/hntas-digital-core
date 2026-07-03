using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class AuditLogResponse
    {
        public List<AuditLog> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public string? HnId { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class AuditLog
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
