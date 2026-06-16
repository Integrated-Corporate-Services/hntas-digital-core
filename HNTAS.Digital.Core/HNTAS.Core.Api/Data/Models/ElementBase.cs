using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class ElementBase
    {
        [BsonElement("elementType")]
        [BsonRepresentation(BsonType.String)]
        public ElementTypeInShort ElementType { get; set; }
    }
}
