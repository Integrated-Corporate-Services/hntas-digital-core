using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class ElementBase
    {
        [BsonElement("elementType")]
        [BsonRepresentation(BsonType.String)]
        public ElementTypeInShort ElementType { get; set; }
    }
}
