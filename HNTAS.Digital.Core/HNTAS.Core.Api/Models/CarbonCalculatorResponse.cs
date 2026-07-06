using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class CarbonCalculatorResponse
    {
        public string HnId { get; set; } = null!;
        public string Uuid { get; set; } = null!;
        public decimal TotalCarbonEmission { get; set; } = 0!;
    }
}
