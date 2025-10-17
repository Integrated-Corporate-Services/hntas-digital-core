using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConnectionType
    {
        [Description("Child connections")]
        ChildConnections = 1,

        [Description("Communal heat network connection")]
        CommunalHeatNetworkConnection = 2,

        [Description("Commercial connection")]
        CommercialConnection = 3,

        [Description("Parent connection")]
        ParentConnection = 4
    }
}
