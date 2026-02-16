using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class NetworkCharacteristics : NetworkDetailBase
    {
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public NetworkDetailsStatus Status { get; set; }

        [BsonElement("id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("heatNetworkType")]
        [BsonRepresentation(BsonType.String)]
        public HeatNetworkType HeatNetworkType { get; set; }
        
        [BsonElement("heatGenerationSourceFor")]
        [BsonRepresentation(BsonType.String)]
        public string? HeatGenerationSourceFor { get; set; }

        [BsonElement("numberOfCommunalFloors")]
        public int? NumberOfCommunalFloors { get; set; }

        [BsonElement("containsPressureBreak")]
        public bool? ContainsPressureBreak { get; set; }

        [BsonElement("isSupplyingOtherHeatNetworks")]
        public bool IsSupplyingOtherHeatNetworks { get; set; }

        [BsonElement("hasCommercialConnections")]
        public bool HasCommercialConnections { get; set; }

        [BsonElement("isSuppliedByADistrictHeatNetwork")]
        public bool IsSuppliedByADistrictHeatNetwork { get; set; }
    }
}
