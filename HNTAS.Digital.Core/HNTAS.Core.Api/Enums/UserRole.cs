using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>
        /// Represents a regulatory contact role.
        /// </summary>
        [Description("Regulatory Contact")]
        RegulatoryContact = 1,

        /// <summary>
        /// Represents a contributor user role.
        /// </summary>
        [Description("Contributor")]
        Contributor = 2,

        /// <summary>
        /// Represents a designer user role.
        /// </summary>
        [Description("Designer")]
        Designer = 3,

        /// <summary>
        /// Represents a contractor user role.
        /// </summary>
        [Description("Contractor")]
        Contractor = 4,

        /// <summary>
        /// Represents a operator user role.
        /// </summary>
        [Description("Operator")]
        Operator = 5,

        /// <summary>
        /// Represents a assessor user role.
        /// </summary>
        [Description("Assessor")]
        Assessor = 6,

        /// <summary>
        /// Represents a certifier user role.
        /// </summary>
        [Description("Certifier")]
        Certifier = 7,

    }
}
