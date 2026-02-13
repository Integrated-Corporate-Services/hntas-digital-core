using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class CounterService : ICounterService
    {
        private readonly IMongoCollection<Counter> _countersCollection;
        private readonly ILogger<CounterService> _logger;
        private readonly IOptions<AWSDocDbSettings> _awsDocDbSettings;

        public CounterService(IOptions<AWSDocDbSettings> awsDocDbSettings, IMongoDatabase mongoDatabase, ILogger<CounterService> logger)
        {
            _awsDocDbSettings = awsDocDbSettings;
            _countersCollection = mongoDatabase.GetCollection<Counter>(awsDocDbSettings.Value.CountersCollectionName);
            _logger = logger;
            _logger.LogInformation("CounterService initialized via Dependency Injection.");
        }

        /// <summary>
        /// Atomically increments a sequence counter and returns its new value.
        /// This method is crucial for generating unique, sequential IDs in MongoDB.
        /// It uses findOneAndUpdate with $inc and upsert:true for atomicity and initialization.
        /// </summary>
        /// <param name="sequenceName">The unique name of the sequence (e.g., "userId_sequence", "orgId_sequence").</param>
        /// <returns>The incremented sequence value.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the counter cannot be retrieved or incremented.</exception>
        public async Task<long> GetNextSequenceValue(string sequenceName)
        {
            // ... (Your startValue logic remains the same)
            long startValue = 1;
            if (string.Equals(sequenceName, "HEATNETWORKID_SEQUENCE", StringComparison.OrdinalIgnoreCase))
                startValue = _awsDocDbSettings.Value.HNSequenceStartValue;
            else if (string.Equals(sequenceName, "ORGID_SEQUENCE", StringComparison.OrdinalIgnoreCase))
                startValue = _awsDocDbSettings.Value.OrgSequenceStartValue;

            var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);

            // 1. Define the stages as BsonDocuments
            var stages = new BsonDocument[]
            {
                new BsonDocument("$set", new BsonDocument("sequenceValue",
                    new BsonDocument("$add", new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$sequenceValue", startValue - 1 }),
                        1
                    })
                ))
            };

            // 2. Explicitly create the PipelineDefinition
            // This ensures the driver knows exactly how to map it to the 'Counter' type.
            PipelineDefinition<Counter, Counter> pipeline = stages;

            var options = new FindOneAndUpdateOptions<Counter, Counter>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            try
            {
                // 3. Use the wrapped 'update' variable
                var counter = await _countersCollection.FindOneAndUpdateAsync(filter, pipeline, options);

                if (counter == null) throw new InvalidOperationException("Counter result was null.");

                return counter.SequenceValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sequence generation failed for {Name}", sequenceName);
                throw;
            }
        }
    }
}

