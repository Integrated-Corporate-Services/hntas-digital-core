using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContributorRole
    {
        [Description("Designated Designer")]
        DesignatedDesigner = 1,
        [Description("Designated Contractor")]
        DesignatedContractor = 2,
        [Description("Designated Operator")]
        DesignatedOperator = 3
    }
}
