using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class SuperUserService : ISuperUserService
    {
        private readonly ILogger<SuperUserService> _logger;
        private readonly IMongoCollection<SuperUser> _superUsersCollection;

        public SuperUserService(ILogger<SuperUserService> logger, IMongoDatabase mongoDatabase, IOptions<AWSDocDbSettings> dbSettings)
        {
            _logger = logger;
            _superUsersCollection = mongoDatabase.GetCollection<SuperUser>(dbSettings.Value.SuperUsersCollectionName);
            _logger.LogInformation("AdminAuthService initialized via Dependency Injection.");
        }

        public async Task<bool> IsSuperUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Normalize email to lowercase for safe matching
            var normalizedEmail = email.Trim().ToLower();

            var filter = Builders<SuperUser>.Filter.And(
                Builders<SuperUser>.Filter.Eq(u => u.EmailId, normalizedEmail),
                Builders<SuperUser>.Filter.Eq(u => u.IsActive, true)
            );

            var count = await _superUsersCollection.CountDocumentsAsync(filter);
            return count > 0;
        }
    }
}
