using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InvitationStatus
    {
        Invited = 1,
        Accepted = 2,
        Rejected = 3
    }
}
