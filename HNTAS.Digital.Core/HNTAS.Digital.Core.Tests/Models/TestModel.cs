using System.Text.Json.Serialization;

namespace HNTAS.Digital.Core.Tests.Models
{
    public class TestModel
    {

        [JsonPropertyName("json_name")]
        public string WithJsonName { get; set; }

        public string WithoutJsonName { get; set; }

    }
}
