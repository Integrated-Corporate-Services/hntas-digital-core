using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IOrganisationService
    {
        Task<Organisation> GetByIdAsync(string orgId);
        Task<bool> IsOrganizationExists(string companiesHouseNumber);
        Task CreateAsync(Organisation newOrganization);
        Task UpdateAsync(string orgId, Organisation updatedOrganization);
        Task RemoveAsync(string orgId);
    }
}
