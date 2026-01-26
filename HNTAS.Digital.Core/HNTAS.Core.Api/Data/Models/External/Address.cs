using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models.External
{
    public class Address
    {
        [BsonElement("addressLine1")]
        public string AddressLine1 { get; set; }

        [BsonElement("addressLine2")]
        public string? AddressLine2 { get; set; } // Added this

        [BsonElement("town")]
        public string? Town { get; set; }

        [BsonElement("county")]
        public string? County { get; set; } // Added this

        [BsonElement("postcode")] // Ensure casing matches your DB ('postcode')
        public string Postcode { get; set; }

        [BsonElement("country")]
        public string? Country { get; set; }
    }
}
