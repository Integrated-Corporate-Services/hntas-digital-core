using HNTAS.Core.Api.Models.Arms.PowerBi;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IArmsPowerBiService
    {
        Task<List<ArmsPowerBiReportResult>> GetPowerBiDataAsync();
    }
}
