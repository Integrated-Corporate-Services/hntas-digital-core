using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;

namespace HNTAS.Core.Api.Services
{
    public class UnitService : IUnitService
    {
        private readonly FrozenDictionary<string, string> _units;

        public UnitService(IOptions<UnitSettings> options)
        {
            _units = options.Value.Units
                .ToFrozenDictionary(
                    x => x.KpiId,
                    x => x.Unit,
                    StringComparer.OrdinalIgnoreCase);
        }

        public string? GetUnit(string kpiId)
        {
            return _units.GetValueOrDefault(kpiId);
        }
    }
}
