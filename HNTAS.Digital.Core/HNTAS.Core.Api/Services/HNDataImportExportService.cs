using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;

namespace HNTAS.Core.Api.Services
{
    public interface IHNDataImportExportService
    {
        Task<List<HeatNetworkExportRow>> GetAllHeatNetworkRowsAsync(CancellationToken ct = default);
    }

    public class HeatNetworkExportRow
    {
        public string UserEmailId { get; set; } = string.Empty;
        public string OneloginId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string OrganisationId { get; set; } = string.Empty;
        public string OrgStreetAddress { get; set; } = string.Empty;
        public string OrgTown { get; set; } = string.Empty;
        public string OrgPostcode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CompaniesHouseNo { get; set; } = string.Empty;
        public string DateOfOrgRegistration { get; set; } = string.Empty;
        public string HnId { get; set; } = string.Empty;
        public string HnName { get; set; } = string.Empty;
        public string DateOfHnRegistration { get; set; } = string.Empty;
        public string RegistrationSource { get; set; } = string.Empty;
        public string ECStreetAddress { get; set; } = string.Empty;
        public string ECTown { get; set; } = string.Empty;
        public string ECPostcode { get; set; } = string.Empty;
        public string ECLatitude { get; set; } = string.Empty;
        public string ECLongitude { get; set; } = string.Empty;               
    }

    public class HNDataImportExportService : IHNDataImportExportService
    {
        private readonly ILogger<HNDataImportExportService> _logger;
        private readonly IMongoCollection<Organisation> _orgCollection;
        private readonly IMongoCollection<BsonDocument> _heatNetworkCollection;
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly IOrganisationService _organisationService;
        private readonly IMongoCollection<BsonDocument> _usersCollection;

        public HNDataImportExportService(
            IMongoDatabase mongoDatabase,
            IOptions<AWSDocDbSettings> dbSettings,
            IUserService userService,
            IHeatNetworkService heatNetworkService,
            IOrganisationService organisationService,
            ILogger<HNDataImportExportService> logger)
        {
            _logger = logger;
            _orgCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
            // Use BsonDocument for collections accessed in aggregation to avoid strict typing projection issues
            _heatNetworkCollection = mongoDatabase.GetCollection<BsonDocument>(dbSettings.Value.HeatNetworksCollectionName);
            _usersCollection = mongoDatabase.GetCollection<BsonDocument>(dbSettings.Value.UsersCollectionName);
            _userService = userService;
            _heatNetworkService = heatNetworkService;
            _organisationService = organisationService;
            _logger.LogInformation("HeatNetworkExportService initialized via Dependency Injection.");
        }

        public async Task<List<HeatNetworkExportRow>> GetAllHeatNetworkRowsAsync(CancellationToken ct=default)
        {            
            var heatNetworks = _heatNetworkService.GetAsync().Result;
            List<HeatNetworkExportRow> docs = new List<HeatNetworkExportRow>();
            foreach(HeatNetwork hn in heatNetworks)
            {
                var theUser = await _userService.GetByIdAsync(hn.CreatedBy);
                var theOrg = await _organisationService.GetByOrgIdAsync(theUser.OrgId);
                var doc = new HeatNetworkExportRow
                {
                    UserEmailId = theUser.EmailId,
                    OneloginId = theUser?.OneLoginId ?? string.Empty,
                    OrganisationName = theOrg.Name,
                    OrganisationId = theOrg.OrgId,
                    OrgStreetAddress = theOrg?.RegisteredAddress?.AddressLine1 ?? string.Empty,
                    OrgTown = theOrg?.RegisteredAddress?.Town ?? string.Empty,
                    OrgPostcode = theOrg?.RegisteredAddress?.Postcode ?? string.Empty,
                    PhoneNumber = theUser?.PreferredContactType.ToString() == HNTAS.Core.Api.Enums.PreferredContactType.Mobile.ToString() ? theUser.MobileNumber : theUser.ContactNumberExtension + " " + theUser.LandlineNumber,
                    CompaniesHouseNo = theOrg?.CompaniesHouseNumber ?? string.Empty,
                    DateOfOrgRegistration = theOrg != null ? theOrg.CreatedAt.ToString("yyyy-MM-dd") : string.Empty,
                    HnId = hn.HnId,
                    HnName = hn.Name,
                    DateOfHnRegistration = hn.CreatedAt.ToString("yyyy-MM-dd") ?? string.Empty,
                    RegistrationSource = hn?.RegistrationSource.ToString(),
                    ECStreetAddress = hn?.Address?.AddressLine1 ?? string.Empty,
                    ECTown = hn?.Address?.Town ?? string.Empty,
                    ECPostcode = hn?.Address?.Postcode ?? string.Empty,
                    ECLatitude = hn?.ECDetails?.Latitude.ToString() ?? string.Empty,
                    ECLongitude = hn?.ECDetails?.Longitude.ToString() ?? string.Empty
                };
                docs.Add(doc); ;
            }

            return docs;
        }
    }
}