using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SoaPhase
    {
        Phase1 = 1,
        Phase2,
        Phase3,
        Phase4,
        Phase5
    }
}
