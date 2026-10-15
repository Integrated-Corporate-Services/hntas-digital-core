using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class HeatNetworkConnections
    {
        //lagacy fields for backward compatibility with existing data in the database
        [BsonElement("isCommunalBuilding")]
        public bool? IsCommunalBuilding { get; set; }
        [BsonElement("noOfCommunalBuilding")]
        public int? NoOfCommunalBuilding { get; set; }
        [BsonElement("isDomesticConsumer")]
        public bool? IsDomesticConsumer { get; set; }
        [BsonElement("noOfDomesticConsumer")]
        public int? NoOfDomesticConsumer { get; set; }
        [BsonElement("isNonDomesticConsumer")]
        public bool? IsNonDomesticConsumer { get; set; }
        [BsonElement("noOfNonDomesticConsumer")]
        public int? NoOfNonDomesticConsumer { get; set; }
        [BsonElement("isDownstreamDistrictHeatNetworkConnections")]
        public bool? IsOtherDistrictNetwork { get; set; }
        [BsonElement("noOfDownstreamDistrictHeatNetworkConnections")]
        public int? NoOfOtherDistrictNetwork { get; set; }

        //New fields for the new data model to capture the number of existing and new connections for each type of connection
        // --- Existing Connections ---
        [BsonElement("noOfExistingCommunalBuilding")]
        public int? NoOfExistingCommunalBuilding { get; set; }

        [BsonElement("noOfExistingHousesOrMaisonettes")]
        public int? NoOfExistingHousesOrMaisonettes { get; set; }

        [BsonElement("noOfExistingNonDomesticBuildings")]
        public int? NoOfExistingNonDomesticBuildings { get; set; }

        [BsonElement("noOfExistingMultiTenantedNonDomestic")]
        public int? NoOfExistingMultiTenantedNonDomestic { get; set; }

        [BsonElement("noOfExistingOtherDistrictNetworks")]
        public int? NoOfExistingOtherDistrictNetworks { get; set; }


        // --- New Connections ---
        [BsonElement("noOfNewCommunalBuilding")]
        public int? NoOfNewCommunalBuilding { get; set; }

        [BsonElement("noOfNewHousesOrMaisonettes")]
        public int? NoOfNewHousesOrMaisonettes { get; set; }

        [BsonElement("noOfNewNonDomesticBuildings")]
        public int? NoOfNewNonDomesticBuildings { get; set; }

        [BsonElement("noOfNewMultiTenantedNonDomestic")]
        public int? NoOfNewMultiTenantedNonDomestic { get; set; }

        [BsonElement("noOfNewOtherDistrictNetworks")]
        public int? NoOfNewOtherDistrictNetworks { get; set; }


        public HeatNetworkConnections()
        {
            IsCommunalBuilding = false;
            NoOfCommunalBuilding = null;
            IsDomesticConsumer = false;
            NoOfDomesticConsumer = null;
            IsNonDomesticConsumer = false;
            NoOfNonDomesticConsumer = null;
            IsOtherDistrictNetwork = false;
            NoOfOtherDistrictNetwork = null;

            NoOfExistingCommunalBuilding = null;
            NoOfExistingHousesOrMaisonettes = null;
            NoOfExistingNonDomesticBuildings = null;
            NoOfExistingMultiTenantedNonDomestic = null;
            NoOfExistingOtherDistrictNetworks = null;

            NoOfNewCommunalBuilding = null;
            NoOfNewHousesOrMaisonettes = null;
            NoOfNewNonDomesticBuildings = null;
            NoOfNewMultiTenantedNonDomestic = null;
            NoOfNewOtherDistrictNetworks = null;
        }
    }
}
