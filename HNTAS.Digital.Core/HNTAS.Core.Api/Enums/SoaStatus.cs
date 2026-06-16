
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SoaStatus 
    {
        [Description("Not started")]
        NotStarted = 1,
        [Description("In Progress")]
        InProgress = 2,
        [Description("SoA completed")]
        SoACompleted = 3,
        [Description("SoA agreed")]
        SoAAgreed = 4,
        [Description("Being assessed")]
        BeingAssessed = 5,        
    }
}
