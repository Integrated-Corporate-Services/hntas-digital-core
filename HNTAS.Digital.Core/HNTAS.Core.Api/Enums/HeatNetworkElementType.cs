using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkElementDisplayType
    {
        [Description("Energy centre")]
        EnergyCentre = 1,

        [Description("Distribution network")]
        DistributionNetwork = 2,

        [Description("Thermal sub station")]
        ThermalSubStation = 3,

        [Description("Communal distribution network")]
        CommunalDistributionNetwork = 4,

        [Description("Consumer connections")]
        ConsumerConnections = 5,

        [Description("Consumer heat systems")]
        ConsumerHeatSystems = 6
    }
}
