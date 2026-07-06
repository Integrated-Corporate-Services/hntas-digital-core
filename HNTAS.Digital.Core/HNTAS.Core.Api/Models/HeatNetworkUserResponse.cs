using HNTAS.Core.Api.Data.Models;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkUserResponse
    {
        [BsonElement("hnId")]
        public string HnId { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        // Expose EC details in the response DTO
        [BsonElement("ecDetails")]
        public ECDetails? ECDetails { get; set; }

        // Expose the structured address instead of a single location string
        [BsonElement("address")]
        public RegisteredAddress? Address { get; set; }
    }
}
