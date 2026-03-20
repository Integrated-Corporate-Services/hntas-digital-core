using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkElementDisplayType
    {
        [Description("Energy centre")]
        EnergyCentre = 1,

        [Description("Substation")]
        Substation = 2,

        [Description("District distribution network")]
        DistrictDistributionNetwork = 3,

        [Description("Communal distribution network")]
        CommunalDistributionNetwork = 4,

        [Description("Consumer connections")]
        ConsumerConnections = 5,

    }
}
