using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrganisationType
    {
        [Description("UK company registered with Companies House")]
        UkCompaniesHouse = 1,
        [Description("Other UK organisation")]
        OtherUkOrganisation = 2,
        [Description("Overseas organisation")]
        OverseasOrganisation = 3
    }
}
