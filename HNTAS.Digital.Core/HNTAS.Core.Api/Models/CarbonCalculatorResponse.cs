namespace HNTAS.Core.Api.Models
{
    public class CarbonCalculatorResponse
    {
        public string HnId { get; set; } = null!;
        public string Uuid { get; set; } = null!;
        public decimal TotalCarbonEmission { get; set; } = 0!;
    }
}
