using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.DataMigrations
{
    [ExcludeFromCodeCoverage]
    public class SeedChpOperationalModes : IDataMigration
    {
        private readonly AWSDocDbSettings _awsDocDbSettings;
        private readonly IMongoCollection<CHPOperationalMode> _chpOperationalModesCollection;
        private readonly ILogger<SeedChpOperationalModes> _logger;

        public SeedChpOperationalModes(
           IOptions<AWSDocDbSettings> awsDocDbSettings,
           ILogger<SeedChpOperationalModes> logger)
        {
            _awsDocDbSettings = awsDocDbSettings.Value;
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. " +
                    "Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }

            if (string.IsNullOrEmpty(awsDocDbSettings.Value.DatabaseName))
            {
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. SeedChpOperationalModes cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(awsDocDbSettings.Value.ChpOperationalModesCollectionName))
            {
                _logger.LogCritical("MongoDB ChpOperationalModesCollectionName is missing in settings. SeedChpOperationalModes cannot initialize.");
                throw new InvalidOperationException("MongoDB ChpOperationalModesCollectionName is not configured. Please check appsettings.json or environment variables.");
            }

            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(awsDocDbSettings.Value.DatabaseName);
                _chpOperationalModesCollection = mongoDatabase.GetCollection<CHPOperationalMode>(awsDocDbSettings.Value.ChpOperationalModesCollectionName);

                _logger.LogInformation("SeedChpOperationalModes initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    awsDocDbSettings.Value.DatabaseName, _chpOperationalModesCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to Db for ChpOperationalModes. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("ChpOperationalModes failed to connect to MongoDB.", ex);
            }
        }

        public async Task RunAsync()
        {
            var count = await _chpOperationalModesCollection.CountDocumentsAsync(FilterDefinition<CHPOperationalMode>.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Seeding ChpOperationalModes collection...");

                var seedData = new List<CHPOperationalMode>
                {
                    new CHPOperationalMode
                    {
                        ChpId = 1,
                        Ident = "export",
                        OperationalMode = "Export only"
                    },
                    new CHPOperationalMode
                    {                        
                        ChpId = 2,
                        Ident = "standard",
                        OperationalMode = "Standard mix"
                    },                    
                    new CHPOperationalMode
                    {
                        ChpId = 3,
                        Ident = "base",
                        OperationalMode = "Own use only"
                    },
                };

                await _chpOperationalModesCollection.InsertManyAsync(seedData);

                _logger.LogInformation("Seeding completed successfully.");
            }
            else
            {
                _logger.LogInformation("ChpOperationalModes collection already contains data. Skipping seeding.");
            }
        }
    }

}
