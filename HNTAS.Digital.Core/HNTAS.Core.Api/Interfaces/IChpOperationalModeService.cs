using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IChpOperationalModeService
    {
        Task<List<CHPOperationalMode>> GetAllAsync();
    }
}
