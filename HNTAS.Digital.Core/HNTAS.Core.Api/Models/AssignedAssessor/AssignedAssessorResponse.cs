using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.NotificationHistory;

namespace HNTAS.Core.Api.Models.AssignedAssessor
{
    public class AssignedAssessorResponse
    {
        public List<AssignedAssessor> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class AssignedAssessor
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? HeatNetworkName { get; set; }
        public string? ElementsAssigned { get; set; }
        public List<string>? ElementsAssignedList { get; set; }
        public UserStatus? Status { get; set; }
        public DateTime? AssessorUpdatedAt { get; set; }
    }
}
