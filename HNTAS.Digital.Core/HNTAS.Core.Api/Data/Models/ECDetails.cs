using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class ECDetails
    {
        // Use Decimal128 representation in MongoDB to preserve precision
        [BsonElement("latitude")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Latitude { get; set; }

        [BsonElement("longitude")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Longitude { get; set; }
    }
}