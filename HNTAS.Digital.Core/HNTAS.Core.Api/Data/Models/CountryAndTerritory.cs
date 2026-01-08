using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class CountryAndTerritory
    {
        /// <summary>
        /// The unique MongoDB document identifier (_id).
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// The common English name of the location (The display text for the user).
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// The full value from the HTML option value (The code used for form submission).
        /// </summary>
        [BsonElement("full_value")]
        public string FullValue { get; set; }
    }
}
