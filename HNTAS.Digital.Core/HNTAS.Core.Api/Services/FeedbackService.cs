using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IMongoCollection<Feedback> _collection;

        public FeedbackService(IMongoDatabase database, IOptions<AWSDocDbSettings> dbSettings)
        {
            _collection = database.GetCollection<Feedback>(dbSettings.Value.FeedbackCollectionName);
        }

        public async Task CreateAsync(CreateFeedbackRequest request)
        {
            var entity = new Feedback
            {
                SatisfactionLevel = request.SatisfactionLevel,
                FeedbackText = request.FeedbackText,
                CreatedAt = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(entity);
        }
    }
}
