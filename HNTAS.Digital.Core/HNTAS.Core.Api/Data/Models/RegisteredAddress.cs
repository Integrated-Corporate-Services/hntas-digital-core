namespace HNTAS.Core.Api.Data.Models
{
    using MongoDB.Bson.Serialization.Attributes;
    using System.ComponentModel.DataAnnotations;

    public class RegisteredAddress
    {
        [Required(ErrorMessage = "Address Line 1 is required.")]
        [BsonElement("addressLine1")]
        public string AddressLine1 { get; set; } = null!;

        [BsonElement("addressLine2")]
        public string? AddressLine2 { get; set; }

        [BsonElement("town")]
        public string? Town { get; set; }

        [BsonElement("county")]
        public string? County { get; set; }

        [Required(ErrorMessage = "Postcode is required.")]
        [BsonElement("postcode")]
        public string Postcode { get; set; } = null!;

        [BsonElement("country")]
        public string? Country { get; set; }
    }
}
