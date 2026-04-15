using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationSource
    {
        [Description("HNTAS")]
        HNTAS = 1,
        [Description("OFGEM")]
        OFGEM = 2
    }
}
