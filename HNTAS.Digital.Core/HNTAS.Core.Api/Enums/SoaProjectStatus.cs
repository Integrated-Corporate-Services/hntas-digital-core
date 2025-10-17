using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SoaProjectStatus
    {
        [Description("In Progress")]
        InProgress = 1,
        [Description("Complete")]
        Complete = 2,
        [Description("Archived")]
        Archived
    }
}
