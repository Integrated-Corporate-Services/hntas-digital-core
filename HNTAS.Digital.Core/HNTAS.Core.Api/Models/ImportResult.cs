using HNTAS.Core.Api.Services;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class ImportResult
    {
        public int RowsProcessed { get; set; }
        public int OrganisationsInserted { get; set; }
        public int OrganisationsUpdated { get; set; }
        public int HeatNetworksInserted { get; set; }
        public int HeatNetworksUpdated { get; set; }
        public int UsersInserted { get; set; }
        public int UsersUpdated { get; set; }
        public List<OfgemDataModelForNotification> DataForExistingOrgOrUser { get; set; } = new List<OfgemDataModelForNotification>();
        public List<OfgemDataModelForNotification> DataForNewOrgOrUser { get; set; } = new List<OfgemDataModelForNotification>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
