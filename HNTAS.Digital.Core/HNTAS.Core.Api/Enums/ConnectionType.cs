using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConnectionType
    {
        [Description("All communal buildings")]
        CommunalBuildings = 1,

        [Description("Individual homes")]
        IndividualHomes = 2,

        [Description("Non-domestic consumers")]
        CommercialConnection = 3,

        [Description("Other district networks")]
        OtherDistrictNetwork = 4
    }
}