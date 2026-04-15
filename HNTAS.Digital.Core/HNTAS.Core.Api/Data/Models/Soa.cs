using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class Soa : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public SoaStatus Status { get; set; }        

        [BsonElement("journeyData")]
        public SoaJourneyData? JourneyData { get; set; }
    }
}
