
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SoaStatus
    {
        [Description("In Progress")]
        InProgress = 1,
        [Description("Submitted")]
        Submitted = 2,
        [Description("Complete")]
        Complete = 3,
        [Description("Archived")]
        Archived = 4
    }
}
