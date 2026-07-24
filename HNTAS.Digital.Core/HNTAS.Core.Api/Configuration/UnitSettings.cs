using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Configuration
{
    [ExcludeFromCodeCoverage]
    public class UnitSettings
    {
        public List<KpiUnit> Units { get; set; } = [];
    }
}
