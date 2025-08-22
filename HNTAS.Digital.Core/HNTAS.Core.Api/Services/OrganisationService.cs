using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IMongoCollection<Organisation> _organizationsCollection;
        private readonly ILogger<OrganisationService> _logger;

        public OrganisationService(IOptions<AWSDocDbSettings> dbSettings, ILogger<OrganisationService> logger)
        {
            _logger = logger;
            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            _logger.LogInformation("Initializing OrganizationService with connection string: {connectionString}", connectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            _organizationsCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
        }

        // Get an organization by its ID
        public async Task<Organisation> GetByIdAsync(string orgId) =>
            await _organizationsCollection.Find(org => org.Id == orgId).FirstOrDefaultAsync();

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
                .Set(o => o.RegisteredAddress, updatedOrganization.RegisteredAddress));

        // Remove an organization by ID
        public async Task RemoveAsync(string orgId) =>
            await _organizationsCollection.DeleteOneAsync(org => org.Id == orgId);
    }
}
