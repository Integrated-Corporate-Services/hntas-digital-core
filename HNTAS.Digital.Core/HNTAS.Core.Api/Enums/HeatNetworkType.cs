using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkType
    {
        [Description("Communal (with an own energy centre)")]
        CommunalWithEnergyCentre = 1,

        [Description("Communal (without own energy centre)")]
        CommunalWithoutEnergyCentre = 2,

        [Description("District (with its own main energy centre)")]
        DistrictWithOwnMainEnergyCentre= 3,

        [Description("District (without own main energy centre)")]
        DistrictWithoutOwnMainEnergyCentre = 4,
    }
}
