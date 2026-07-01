using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class AuditLogRequest
    {
        public string HnId { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
