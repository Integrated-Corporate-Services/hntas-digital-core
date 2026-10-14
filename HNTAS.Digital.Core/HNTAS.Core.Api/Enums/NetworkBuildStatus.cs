using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NetworkBuildStatus
    {
        [Description("It is an existing heat network")]
        Existing = 1,

        [Description("It is a new heat network")]
        New = 2,

        [Description("It is a new and existing heat network")]
        NewAndExisting = 3
    }
}
