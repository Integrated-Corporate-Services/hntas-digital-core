using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IHeatRecoverySourceService
    {
        Task<List<HeatRecovery>> GetAllAsync();
    }
}
