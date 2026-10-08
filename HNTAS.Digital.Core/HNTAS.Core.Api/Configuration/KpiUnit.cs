using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Configuration
{
    [ExcludeFromCodeCoverage]
    public class KpiUnit
    {
        public string KpiId { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
