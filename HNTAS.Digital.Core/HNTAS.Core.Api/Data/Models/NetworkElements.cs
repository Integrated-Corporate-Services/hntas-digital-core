using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class NetworkElements : NetworkDetailBase
    {
        [BsonElement("networkElementStatus")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus NetworkElementStatus { get; set; }
        [BsonElement("elementSoaStatus")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus ElementSoaStatus { get; set; }

        [BsonElement("elements")]
        public List<Element>? Elements { get; set; } = [];
        [BsonElement("elementsGroup")]
        public List<ElementGroup> ElementsGroup { get; set; } = [];
    }    
}
