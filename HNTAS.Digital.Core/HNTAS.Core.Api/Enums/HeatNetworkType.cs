using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkType
    {
        [Description("Communal (with an integral energy centre)")]
        CommunalWithIntegralEC = 1,

        [Description("Communal (supplied by a separate upstream heat network)")]
        CommunalWithSeparateUpstreamHN = 2,

        [Description("District (with its own main energy centre)")]
        DistrictWithOwnEC = 3,

        [Description("District (supplied by a separate upstream heat network)")]
        DistrictWithSeparateUpstreamHN = 4,
    }
}
