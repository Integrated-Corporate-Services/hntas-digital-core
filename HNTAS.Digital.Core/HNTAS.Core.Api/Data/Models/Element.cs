using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class Element : ElementBase
    {        
        [BsonElement("elementId")]
        public string? ElementId { get; set; }        

        [BsonElement("networkElementInstanceName")]
        [BsonRepresentation(BsonType.String)]
        public string? NetworkElementInstanceName { get; set; }
        
    }    
}
