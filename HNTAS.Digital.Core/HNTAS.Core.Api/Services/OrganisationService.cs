using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HNTAS.Core.Api.Services
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IMongoCollection<Organisation> _organizationsCollection;
        private readonly ILogger<OrganisationService> _logger;

        public OrganisationService(IOptions<AWSDocDbSettings> dbSettings, ILogger<OrganisationService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _organizationsCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
            _logger.LogInformation("OrganisationService initialized via Dependency Injection.");
        }        

        // Get an organization by its Companys House Number
        public async Task<Organisation> GetByCompanyHouseNumberAsync(string companyHouseNumber) =>
            await _organizationsCollection.Find(org => org.CompaniesHouseNumber == companyHouseNumber).FirstOrDefaultAsync();

        // Get an organization by its OrgID
        public async Task<Organisation> GetByOrgIdAsync(string orgId) =>
            await _organizationsCollection.Find(org => org.OrgId == orgId).FirstOrDefaultAsync();

        // Check if an organization exists by Companies House Number
        public async Task<bool> IsOrganizationExists(string companiesHouseNumber) =>
            await _organizationsCollection.Find(org => org.CompaniesHouseNumber == companiesHouseNumber).AnyAsync();

        // Create a new organization
        public async Task CreateAsync(Organisation newOrganization) =>
            await _organizationsCollection.InsertOneAsync(newOrganization);

        // Update an existing organization
        public async Task UpdateAsync(string orgId, Organisation updatedOrganization) =>
            await _organizationsCollection.UpdateOneAsync(org => org.Id == orgId, Builders<Organisation>.Update
                .Set(o => o.Type, updatedOrganization.Type)
                .Set(o => o.CompaniesHouseNumber, updatedOrganization.CompaniesHouseNumber)
                .Set(o => o.Name, updatedOrganization.Name)
                .Set(o => o.RegisteredAddress, updatedOrganization.RegisteredAddress)
                .Set(o => o.LastModifiedBy, updatedOrganization.LastModifiedBy)
                .Set(o => o.LastModifiedAt, updatedOrganization.LastModifiedAt)
                .Set(o => o.HnIds, updatedOrganization.HnIds)
                .Set(o => o.RpUserId, updatedOrganization.RpUserId));

        // Remove an organization by ID
        public async Task RemoveAsync(string orgId) =>
            await _organizationsCollection.DeleteOneAsync(org => org.Id == orgId);


        public async Task<bool> ExistsByDetailsAsync(string name, string postCode, string country)
        {
            var filterBuilder = Builders<Organisation>.Filter;

            // Case-insensitive regex for name, postcode and country
            var nameRegex = new BsonRegularExpression($"^{Regex.Escape(name)}$", "i");

            var postCodeRegex = new BsonRegularExpression($"^{Regex.Escape(postCode)}$", "i");
            var countryRegex = new BsonRegularExpression($"^{Regex.Escape(country)}$", "i");

            var combinedFilter = filterBuilder.And(
                filterBuilder.Regex(o => o.Name, nameRegex),
                filterBuilder.Regex("registeredAddress.postcode", postCodeRegex),
                filterBuilder.Regex("registeredAddress.country", countryRegex)
            );

            var existingOrganisation = await _organizationsCollection
                .Find(combinedFilter)
                .Limit(1)
                .FirstOrDefaultAsync();

            return existingOrganisation != null;
        }

        public async Task<Organisation?> GetByOrgIdOrNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return null;
            }

            string pattern = $"^{searchTerm}$";
            var regex = new BsonRegularExpression(pattern, "i");

            var builder = Builders<Organisation>.Filter;

            var orgIdFilter = builder.Regex(x => x.OrgId, regex);

            var nameFilter = builder.Regex(x => x.Name, regex);

            var combinedFilter = builder.Or(orgIdFilter, nameFilter);

            return await _organizationsCollection
                .Find(combinedFilter)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(string orgId, string hnId)
        {
            var filter = Builders<Organisation>.Filter.Eq(o => o.OrgId, orgId);
            var update = Builders<Organisation>.Update.AddToSet(o => o.HnIds, hnId);
            await _organizationsCollection.UpdateOneAsync(filter, update);
        }
    }
}
