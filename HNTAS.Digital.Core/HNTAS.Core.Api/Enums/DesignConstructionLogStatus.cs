using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DesignConstructionLogStatus
    {        
        [Description("In Progress")]
        InProgress = 1,
        [Description("Submitted")]
        Submitted = 2,
        [Description("Complete")]
        Complete = 3,
        [Description("Archived")]
        Archived = 4,
        [Description("Ready to start")]
        ReadyToStart = 5,
        [Description("Cannot start yet")]
        CannotStartYet = 6,
        [Description("Incomplete")]
        Incomplete = 7,
    }
}
