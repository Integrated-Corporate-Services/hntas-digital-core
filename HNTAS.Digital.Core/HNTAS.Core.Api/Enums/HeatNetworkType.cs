using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeatNetworkType
    {
        [Description("City-scale district heating network (CSDH)")]
        CityScaleDistrictHeatingNetwork = 1,

        [Description("Development led district heating network (DLDH)")]
        DevelopmentLedDistrictHeatingNetwork = 2,

        [Description("Large communal heat network (c.300 consumers) (LD)")]
        LargeCommunalHeatNetwork = 3,

        [Description("Medium communal heat network (c.100 consumers) (MC)")]
        MediumCommunalHeatNetwork = 4,

        [Description("Small communal heat network (c. 50 consumers) (SC)")]
        SmallCommunalHeatNetwork = 5,

        [Description("Other")]
        Other = 6
    }
}
