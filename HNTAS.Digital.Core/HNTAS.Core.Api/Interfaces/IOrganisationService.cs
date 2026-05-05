using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IOrganisationService
    {
        Task<Organisation> GetByIdAsync(string orgId);
        Task<Organisation> GetByOrgIdAsync(string orgId);
        Task<bool> IsOrganizationExists(string companiesHouseNumber);
        Task CreateAsync(Organisation newOrganization);
        Task UpdateAsync(string orgId, Organisation updatedOrganization);
        Task RemoveAsync(string orgId);
        Task<bool> ExistsByDetailsAsync(string name, string postCode, string country);
        Task<Organisation?> GetByOrgIdOrNameAsync(string searchTerm);
        Task<Organisation> GetByCompanyHouseNumberAsync(string companyHouseNumber);
        Task UpdateAsync(string orgId, string hnId);
    }
}
