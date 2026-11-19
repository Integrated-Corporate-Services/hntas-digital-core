using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MainEnergyCentreStatus
    {
        [Description("Existing")]
        Existing = 1,

        [Description("New")]
        New = 2
    }
}
