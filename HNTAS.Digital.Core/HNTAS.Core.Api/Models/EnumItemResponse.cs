using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class EnumItemResponse
    {
        public int Value { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
