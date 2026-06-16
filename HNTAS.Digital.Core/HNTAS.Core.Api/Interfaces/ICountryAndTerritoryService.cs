using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ICountryAndTerritoryService
    {
        Task<List<CountryAndTerritory>> GetAllAsync();
        Task<CountryAndTerritory?> GetByIdAsync(string id);
        Task<bool> ExistsAsync(string name);
    }
}
