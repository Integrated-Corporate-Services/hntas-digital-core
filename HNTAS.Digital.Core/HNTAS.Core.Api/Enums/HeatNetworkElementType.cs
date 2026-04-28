using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkElementType
    {
        [Description("Energy centre")]
        EnergyCentre = 1,

        [Description("Substation")]
        Substation = 2,

        [Description("District distribution")]
        DistrictDistribution = 3,

        [Description("Communal distribution")]
        CommunalDistribution = 4,

        [Description("Consumer connection")]
        ConsumerConnection = 5,
    }
}
