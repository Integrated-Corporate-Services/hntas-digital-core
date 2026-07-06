using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models.External
{
    [ExcludeFromCodeCoverage]
    public class EnergyCentreDetails
    {
        [BsonElement("latitude")]
        public string Latitude { get; set; }

        [BsonElement("longitude")]
        public string Longitude { get; set; }

        [BsonElement("address")]
        public Address Address { get; set; }
    }
}
