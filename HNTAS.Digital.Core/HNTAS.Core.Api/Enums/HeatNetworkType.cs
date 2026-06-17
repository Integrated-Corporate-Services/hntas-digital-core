using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkType
    {
        [Description("Unset")]
        Unset = 0,

        [Description("Communal")]
        Communal = 1,        

        [Description("District")]
        District= 2
    }
}
