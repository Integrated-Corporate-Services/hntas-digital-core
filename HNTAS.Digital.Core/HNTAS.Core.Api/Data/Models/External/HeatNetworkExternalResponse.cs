using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models.External
{
    public class HeatNetworkExternalResponse
    {
        public string Id { get; set; }

        [BsonElement("hnId")]
        public string HnId { get; set; }

        [BsonElement("hnName")]
        public string HnName { get; set; }

        [BsonElement("registrationSource")]
        public string RegistrationSource { get; set; }

        [BsonElement("energyCentre")]
        public EnergyCentreDetails EnergyCentre { get; set; }

        [BsonElement("pathway")]
        public string Pathway { get; set; }

        [BsonElement("soa")]
        public SoaResponse Soa { get; set; }

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("rpDetails")]
        public OrgDetails RpDetails { get; set; }
    }


}
