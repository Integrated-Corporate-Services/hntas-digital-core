using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models
{
    public class NetworkElements : NetworkDetailBase
    {
        [BsonElement("networkElementStatus")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus NetworkElementStatus { get; set; }
        [BsonElement("elementSoaStatus")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus ElementSoaStatus { get; set; }

        [BsonElement("elements")]
        public List<Element> Elements { get; set; } = [];
        [BsonElement("distributionNetworksAndSubstations")]
        public DistributionNetworksAndSubstations? DistributionNetworksAndSubstations { get; set; }
    }

    public class DistributionNetworksAndSubstations
    {
        [BsonElement("isAnyDistrictSubstations")]
        public bool? IsAnyDistrictSubstations { get; set; }
        [BsonElement("numberOfSubstations")]
        [BsonRepresentation(BsonType.Int32)]
        public int? NumberOfSubstations { get; set; }
        [BsonElement("numberOfDistributionNetworks")]
        [BsonRepresentation(BsonType.Int32)]
        public int? NumberOfDistributionNetworks { get; set; }
    }
}
