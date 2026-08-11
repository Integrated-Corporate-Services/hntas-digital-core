using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Milestone
    {
        Milestone2,
        Milestone3A,
        Milestone3B,
        Milestone4,
        Milestone5,
    }
}
