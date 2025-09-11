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
        DesignatedOperator = 3,
        [Description("Contributing Designer")]
        ContributingDesigner = 4,
        [Description("Contributing Contractor")]
        ContributingContractor = 5,
        [Description("Contributing Operator")]
        ContributingOperator = 6,
        [Description("Assessor")]
        Assessor = 7
    }
}
