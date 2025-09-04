using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SoaStage
    {
        Stage1 = 1,
        Stage2,
        Stage3,
        Stage4,
        Stage5,
        Stage6,
        Stage7,
        Stage8
    }
}
