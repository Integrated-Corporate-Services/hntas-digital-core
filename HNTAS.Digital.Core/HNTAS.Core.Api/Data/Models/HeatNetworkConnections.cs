namespace HNTAS.Core.Api.Data.Models
{
    public class HeatNetworkConnections
    {
        public bool IsCommunalBuilding { get; set; }
        public int? NoOfCommunalBuilding { get; set; }
        public bool IsDomesticConsumer { get; set; }
        public int? NoOfDomesticConsumer { get; set; }
        public bool IsNonDomesticConsumer { get; set; }
        public int? NoOfNonDomesticConsumer { get; set; }
        public bool IsDownstreamDistrictHeatNetworkConnections { get; set; }
        public int? NoOfDownstreamDistrictHeatNetworkConnections { get; set; }
        public bool IsUpstreamDistrictHeatNetworkConnections { get; set; }
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
