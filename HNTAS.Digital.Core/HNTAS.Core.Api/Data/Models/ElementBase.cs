using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class ElementBase
    {
        [BsonElement("elementType")]
        public string? ElementType { get; set; }
    }
}
