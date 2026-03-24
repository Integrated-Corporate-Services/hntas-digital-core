using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>
        /// Represents a responsible person role.
        /// </summary>
        [Description("Responsible Person")]
        ResponsiblePerson = 1,

        /// <summary>
        /// Represents a HNTAS Coordinator role.
        /// </summary>
        [Description("HNTAS Coordinator")]
        Coordinator = 2,

        /// <summary>
        /// Represents a Designated Duty Holder user role.
        /// </summary>
        [Description("Designated Duty Holder")]
        DesignatedDutyHolder = 3,

        /// <summary>
        /// Represents a contributor user role.
        /// </summary>
        [Description("Contributor")]
        Contributor = 4,

        /// <summary>
        /// Represents a assessor user role.
        /// </summary>
        [Description("Assessor")]
        Assessor = 5,

        /// <summary>
        /// Represents a certifier user role.
        /// </summary>
        [Description("Certifier")]
        Certifier = 6
    }
}
