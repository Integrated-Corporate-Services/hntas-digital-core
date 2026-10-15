using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EnergyCentreType
    {
        [Description("Yes, it supplies multiple buildings")]
        SuppliesMultipleBuildings = 1,

        [Description("No, multiple buildings are supplied by a separate district network")]
        SeparateDistrictNetworkMultipleBuildings = 2,

        [Description("No, the energy centre supplies one communal building only")]
        SuppliesOneCommunalBuildingOnly = 3,

        [Description("No, a single communal building is supplied by a separate district network")]
        SingleCommunalBuildingSeparateDistrictNetwork = 4
    }
}
