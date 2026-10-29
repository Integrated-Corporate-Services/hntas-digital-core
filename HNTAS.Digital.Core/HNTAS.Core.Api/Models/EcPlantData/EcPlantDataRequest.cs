using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Models.EcPlantData
{
    public class EcPlantDataRequest
    {
        public string? HnId { get; set; }
        public string? ElementId { get; set; }
        public EnergyCentrePlantData? EnergyCentrePlantData { get; set; }
    }
}
