using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Assessor;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HNTAS.Core.Api.Services
{
    public class AssessorService : IAssessorService
    {
        private readonly ILogger<AssessorService> _logger;
        private readonly IMongoCollection<Assessor> _assessorCollection;

        public AssessorService(ILogger<AssessorService> logger, IMongoDatabase mongoDatabase, IOptions<AWSDocDbSettings> dbSettings)
        {
            _logger = logger;
            _assessorCollection = mongoDatabase.GetCollection<Assessor>(dbSettings.Value.AssessorsCollectionName);
            _logger.LogInformation("AssessorService initialized via Dependency Injection.");
        }

        public async Task<List<AssessorSearchResult>> GetAssessorSuggestionsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<AssessorSearchResult>();

            var escapedSearchTerm = Regex.Escape(searchTerm);

            var filter = Builders<Assessor>.Filter.And(
                Builders<Assessor>.Filter.Eq(u => u.Status, UserStatus.Active),
                Builders<Assessor>.Filter.Or(
                    Builders<Assessor>.Filter.Regex(u => u.FirstName, new BsonRegularExpression(escapedSearchTerm, "i")),
                    Builders<Assessor>.Filter.Regex(u => u.LastName, new BsonRegularExpression(escapedSearchTerm, "i")),
                    Builders<Assessor>.Filter.Regex(u => u.Email, new BsonRegularExpression(escapedSearchTerm, "i")),
                    Builders<Assessor>.Filter.Regex(u => u.FullNameWithEmail, new BsonRegularExpression(escapedSearchTerm, "i"))
                )
            );
            try
            {
                var assessors = await _assessorCollection.Find(filter).ToListAsync();
                return assessors.Select(a => new AssessorSearchResult
                {
                    Id = a.Id!,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    FullName = a.FullName,
                    FullNameWithEmail = a.FullNameWithEmail
                }).ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
