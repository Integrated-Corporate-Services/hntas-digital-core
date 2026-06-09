using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ElementTypeInShort
    {
        [Description("Energy centre")]
        EC = 1,

        [Description("Substation")]
        SS = 2,

        [Description("District distribution")]
        DDN = 3,

        [Description("Communal distribution")]
        CDN = 4,

        [Description("Consumer connection")]
        CC = 5,
    }
}
