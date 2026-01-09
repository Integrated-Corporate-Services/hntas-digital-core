using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Assessor;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class AssessorService : IAssessorService
    {
        private readonly ILogger<AssessorService> _logger;
        private readonly IMongoCollection<Assessor> _assessorCollection;

        public AssessorService(ILogger<AssessorService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _assessorCollection = mongoDatabase.GetCollection<Assessor>("Assessors");
            _logger.LogInformation("AssessorService initialized via Dependency Injection.");
        }

        public async Task<List<AssessorSearchResult>> GetAssessorSuggestionsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<AssessorSearchResult>();

            // 'i' for case-insensitive, '^' for "starts with" to utilize indexes
            var regex = new BsonRegularExpression($"^{searchTerm}", "i");

            var filter = Builders<Assessor>.Filter.And(
                Builders<Assessor>.Filter.Eq(a => a.Status, Enums.UserStatus.Active),
                Builders<Assessor>.Filter.Or(
                    Builders<Assessor>.Filter.Regex(a => a.FirstName, regex),
                    Builders<Assessor>.Filter.Regex(a => a.LastName, regex)
                )
            );

            // Limit results to 10 to keep the typeahead snappy
            var results = await _assessorCollection.Find(filter)
                .Limit(10)
                .ToListAsync();

            return results.Select(a => new AssessorSearchResult
            {
                Id = a.Id,
                FullName = a.FullName
            }).ToList();
        }

        public async Task CreateAssessorAsync(Assessor assessor)
        {
            await _assessorCollection.InsertOneAsync(assessor);
        }



    }
}
