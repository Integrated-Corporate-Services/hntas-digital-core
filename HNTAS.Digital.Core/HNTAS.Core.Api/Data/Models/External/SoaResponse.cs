using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models.External
{
    public class SoaResponse
    {
        [BsonElement("status")]
        public string Status { get; set; }
    }
}
