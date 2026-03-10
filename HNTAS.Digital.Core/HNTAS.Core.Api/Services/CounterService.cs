using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class CounterService : ICounterService
    {
        private readonly IMongoCollection<Counter> _countersCollection;
        private readonly ILogger<CounterService> _logger;

        public CounterService(IOptions<AWSDocDbSettings> awsDocDbSettings, IMongoDatabase mongoDatabase, ILogger<CounterService> logger)
        {
            _countersCollection = mongoDatabase.GetCollection<Counter>(awsDocDbSettings.Value.CountersCollectionName);
            _logger = logger;
            _logger.LogInformation("CounterService initialized via Dependency Injection.");
        }

        /// <summary>
        /// Atomically increments a sequence counter and returns its new value.
        /// This method is crucial for generating unique, sequential IDs in MongoDB.
        /// Note: Counter documents should be pre-initialized with starting values (e.g., 2000001)
        /// to ensure the first returned value is 2000001.
        /// </summary>
        /// <param name="sequenceName">The unique name of the sequence (e.g., "userId_sequence", "orgId_sequence").</param>
        /// <returns>The incremented sequence value.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the counter cannot be retrieved or incremented.</exception>
        public async Task<long> GetNextSequenceValue(string sequenceName)
        {
            _logger.LogDebug("Attempting to get next sequence value for counter: '{SequenceName}'.", sequenceName);

            var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);
            
            // Use only Inc - cannot combine with SetOnInsert on same field
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);

            var options = new FindOneAndUpdateOptions<Counter, Counter>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            try
            {
                var counter = await _countersCollection.FindOneAndUpdateAsync(filter, update, options);

                if (counter == null) throw new InvalidOperationException("Counter result was null.");

                // Handle first-time initialization: if counter was just created, set to desired starting value
                if (counter.SequenceValue < 2000001)
                {
                    _logger.LogWarning("Counter '{SequenceName}' was not pre-initialized. Setting starting value to 2000001.", sequenceName);
                    var setUpdate = Builders<Counter>.Update.Set(c => c.SequenceValue, 2000001);
                    counter = await _countersCollection.FindOneAndUpdateAsync(filter, setUpdate, 
                        new FindOneAndUpdateOptions<Counter, Counter> { ReturnDocument = ReturnDocument.After });
                    
                    if (counter == null) throw new InvalidOperationException("Counter initialization failed.");
                }

                _logger.LogDebug("Sequence value for '{SequenceName}' is now: {Value}", sequenceName, counter.SequenceValue);

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

