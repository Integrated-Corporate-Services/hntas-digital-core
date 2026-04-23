using HNTAS.Core.Api.Models.NotificationHistory;

namespace HNTAS.Core.Api.Models.AssignedAssessor
{
    public class AssignedAssessor
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? HeatNetworkName { get; set; }
        public string? ElementAssigned { get; set; }
        public string? Status { get; set; }               
    }

    public class AssignedAssessorData
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? HeatNetworkName { get; set; }
        public List<string>? ElementAssigned { get; set; }
        public string? Status { get; set; }
    }

    public class AssignedAssessorResponse
    {
        public List<AssignedAssessorData> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
