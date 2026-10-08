using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Arms.Dashboard
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkDetailsResponse
    {
        public string HnId { get; set; } = string.Empty;
        public string NetworkName { get; set; } = string.Empty;
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }

        // The actual KPI data grouped by Element
        public List<ElementGroupDto> GroupedElements { get; set; } = new();

        public List<AggregatedKpi>? AggregatedKpis { get; set; } = new();

        public Dictionary<string, CarbonInputUiDisplay>? CarbonCalculationInputs { get; set; }
        public decimal? TotalCarbonEmission { get; set; }

        // Pagination Metadata
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalElements { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class CarbonInputUiDisplay
    {
        public string Label { get; set; } = null!;
        public double Value { get; set; }

        public string? Unit { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class AggregatedKpi
    {
        public string KpiName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Status { get; set; } = string.Empty;

        public string? Unit { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ElementGroupDto
    {
        public string ElementId { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public List<KpiDetailDto> Kpis { get; set; } = new();
    }

    [ExcludeFromCodeCoverage]
    public class KpiDetailDto
    {
        public string KpiName { get; set; } = string.Empty; // The Dictionary Key
        public double Value { get; set; }
        public string? Unit { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsImputed { get; set; }
        public string? ImputationDetails { get; set; }
    }
}
