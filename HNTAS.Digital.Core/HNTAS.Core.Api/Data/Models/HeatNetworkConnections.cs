using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetworkConnections
    {
        [BsonElement("isCommunalBuilding")]
        public bool IsCommunalBuilding { get; set; }

        [BsonElement("numberOfCommunalBuildings")]
        [BsonIgnoreIfNull]
        public int? NumberOfCommunalBuildings { get; set; }

        [BsonElement("isDomesticConsumer")]
        public bool IsDomesticConsumer { get; set; }

        [BsonElement("numberOfDomesticConsumers")]
        [BsonIgnoreIfNull]
        public int? NumberOfDomesticConsumers { get; set; }

        [BsonElement("isNonDomesticConsumer")]
        public bool IsNonDomesticConsumer { get; set; }

        [BsonElement("numberOfNonDomesticConsumers")]
        [BsonIgnoreIfNull]
        public int? NumberOfNonDomesticConsumers { get; set; }

        [BsonElement("isDownstreamDistrictHeatNetworkConnections")]
        public bool IsDownstreamDistrictHeatNetworkConnections { get; set; }

        [BsonElement("numberOfDownstreamDistrictHeatNetworkConnections")]
        [BsonIgnoreIfNull]
        public int? NumberOfDownstreamDistrictHeatNetworkConnections { get; set; }

        [BsonElement("isUpstreamDistrictHeatNetworkConnections")]
        public bool IsUpstreamDistrictHeatNetworkConnections { get; set; }

        [BsonElement("numberOfUpstreamDistrictHeatNetworkConnections")]
        [BsonIgnoreIfNull]
        public int? NumberOfUpstreamDistrictHeatNetworkConnections { get; set; }

        public HeatNetworkConnections()
        {
            IsCommunalBuilding = false;
            NumberOfCommunalBuildings = null;
            IsDomesticConsumer = false;
            NumberOfDomesticConsumers = null;
            IsNonDomesticConsumer = false;
            NumberOfNonDomesticConsumers = null;
            IsDownstreamDistrictHeatNetworkConnections = false;
            NumberOfDownstreamDistrictHeatNetworkConnections = null;
            IsUpstreamDistrictHeatNetworkConnections = false;
            NumberOfUpstreamDistrictHeatNetworkConnections = null;
        }
    }
}
