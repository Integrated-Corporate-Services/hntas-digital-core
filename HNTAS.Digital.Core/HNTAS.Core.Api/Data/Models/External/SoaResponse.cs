using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models.External
{
    [ExcludeFromCodeCoverage]
    public class SoaResponse
    {
        [BsonElement("status")]
        public string Status { get; set; }
    }
}
