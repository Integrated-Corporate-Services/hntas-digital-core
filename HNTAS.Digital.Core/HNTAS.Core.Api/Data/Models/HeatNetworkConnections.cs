using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetworkConnections
    {
        [BsonElement("isCommunalBuilding")]
        public bool IsCommunalBuilding { get; set; }
        [BsonElement("noOfCommunalBuilding")]
        public int? NoOfCommunalBuilding { get; set; }
        [BsonElement("isDomesticConsumer")]
        public bool IsDomesticConsumer { get; set; }
        [BsonElement("noOfDomesticConsumer")]
        public int? NoOfDomesticConsumer { get; set; }
        [BsonElement("isNonDomesticConsumer")]
        public bool IsNonDomesticConsumer { get; set; }
        [BsonElement("noOfNonDomesticConsumer")]
        public int? NoOfNonDomesticConsumer { get; set; }
        [BsonElement("isDownstreamDistrictHeatNetworkConnections")]
        public bool IsDownstreamDistrictHeatNetworkConnections { get; set; }
        [BsonElement("noOfDownstreamDistrictHeatNetworkConnections")]
        public int? NoOfDownstreamDistrictHeatNetworkConnections { get; set; }
        [BsonElement("isUpstreamDistrictHeatNetworkConnections")]
        public bool IsUpstreamDistrictHeatNetworkConnections { get; set; }
        [BsonElement("noOfUpstreamDistrictHeatNetworkConnections")]
        public int? NoOfUpstreamDistrictHeatNetworkConnections { get; set; }

        public HeatNetworkConnections()
        {
            IsCommunalBuilding = false;
            NoOfCommunalBuilding = null;
            IsDomesticConsumer = false;
            NoOfDomesticConsumer = null;
            IsNonDomesticConsumer = false;
            NoOfNonDomesticConsumer = null;
            IsDownstreamDistrictHeatNetworkConnections = false;
            NoOfDownstreamDistrictHeatNetworkConnections = null;
            IsUpstreamDistrictHeatNetworkConnections = false;
            NoOfUpstreamDistrictHeatNetworkConnections = null;
        }
    }
}
