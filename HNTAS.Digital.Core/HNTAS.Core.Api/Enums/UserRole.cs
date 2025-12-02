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
        /// Represents a contributor user role.
        /// </summary>
        [Description("Contributor")]
        Contributor = 3,

        /// <summary>
        /// Represents a designer user role.
        /// </summary>
        [Description("Designer")]
        Designer = 4,

        /// <summary>
        /// Represents a contractor user role.
        /// </summary>
        [Description("Contractor")]
        Contractor = 5,

        /// <summary>
        /// Represents a operator user role.
        /// </summary>
        [Description("Operator")]
        Operator = 6,

        /// <summary>
        /// Represents a assessor user role.
        /// </summary>
        [Description("Assessor")]
        Assessor = 7,

        /// <summary>
        /// Represents a certifier user role.
        /// </summary>
        [Description("Certifier")]
        Certifier = 8,

    }
}
