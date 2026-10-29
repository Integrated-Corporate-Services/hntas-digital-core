using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IFuelTypeService
    {
        Task<List<FuelType>> GetAllAsync();
    }
}
