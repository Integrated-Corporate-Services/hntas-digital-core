using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KPIAssessmentStatus
    {
        [Description("Pass")]
        /// <summary>
        /// Value is within limits and meets/exceeds the target threshold.
        /// </summary>
        Pass = 1,

        [Description("Fail")]
        /// <summary>
        /// Value is within limits but does not meet the target threshold.
        /// </summary>
        Fail = 2,

        [Description("Outside Limit")]
        /// <summary>
        /// Value is physically or logically outside the allowed Upper/Lower bounds.
        /// </summary>
        OutsideLimit = 3
    }
}
