using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models
{
    public class NetworkElements : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus Status { get; set; }

        [BsonElement("elements")]
        public List<Element> Elements { get; set; } = [];        
    }
    
}
