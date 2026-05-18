using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Element : ElementBase
    {        
        [BsonElement("elementId")]
        public string? ElementId { get; set; }        

        [BsonElement("networkElementInstanceName")]
        [BsonRepresentation(BsonType.String)]
        public string? NetworkElementInstanceName { get; set; }
        
    }    
}
