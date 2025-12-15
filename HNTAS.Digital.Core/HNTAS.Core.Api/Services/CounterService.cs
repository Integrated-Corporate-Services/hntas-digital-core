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
        private readonly ILogger _logger;

        public CounterService(IOptions<AWSDocDbSettings> awsDocDbSettings, IMongoDatabase mongoDatabase, ILogger logger)
        {
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
            _logger.LogDebug("Attempting to get next sequence value for counter: '{SequenceName}'.", sequenceName);

            // Filter to find the specific counter document by its _id
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);

            // Update operation: increment the 'sequence_value' field by 1
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);

            // Options for the findOneAndUpdate operation
            var options = new FindOneAndUpdateOptions<Counter, Counter>
            {
                ReturnDocument = ReturnDocument.After, // Return the document *after* the update has been applied
                IsUpsert = true // If a document matching the filter doesn't exist, create it.
                                // For a new counter, SequenceValue will effectively start at 0, then become 1.
            };

            try
            {
                // Execute the atomic findOneAndUpdate operation
                var counter = await _countersCollection.FindOneAndUpdateAsync(filter, update, options);

                // Defensive check: counter should never be null if IsUpsert is true and operation is successful
                if (counter == null)
                {
                    _logger.LogError("Failed to retrieve or create counter document for sequence '{SequenceName}' despite upsert option. This is unexpected.", sequenceName);
                    throw new InvalidOperationException($"Failed to get or create counter for sequence '{sequenceName}'. The database operation did not return a document.");
                }

                _logger.LogDebug("Next sequence value for '{SequenceName}' is {SequenceValue}.", sequenceName, counter.SequenceValue);
                return counter.SequenceValue;
            }
            catch (MongoException ex) // Catch specific MongoDB driver exceptions
            {
                _logger.LogError(ex, "MongoDB error occurred while getting next sequence value for '{SequenceName}'.", sequenceName);
                throw new InvalidOperationException($"Database error generating sequence '{sequenceName}'. See inner exception for details.", ex);
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                _logger.LogError(ex, "An unexpected error occurred while getting next sequence value for '{SequenceName}'.", sequenceName);
                throw new InvalidOperationException($"Failed to generate sequence '{sequenceName}' due to an unexpected error. See inner exception for details.", ex);
            }
        }
    }
}

