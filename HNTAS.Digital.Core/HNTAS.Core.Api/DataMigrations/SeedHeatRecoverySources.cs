using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.DataMigrations
{
    [ExcludeFromCodeCoverage]
    public class SeedHeatRecoverySources : IDataMigration
    {
        private readonly AWSDocDbSettings _awsDocDbSettings;
        private readonly IMongoCollection<HeatRecovery> _heatRecoverySourcesCollection;
        private readonly ILogger<SeedHeatRecoverySources> _logger;

        public SeedHeatRecoverySources(
           IOptions<AWSDocDbSettings> awsDocDbSettings,
           ILogger<SeedHeatRecoverySources> logger)
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
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. SeedHeatRecoverySources cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(awsDocDbSettings.Value.HeatRecoverySourcesCollectionName))
            {
                _logger.LogCritical("MongoDB HeatRecoverySourcesCollectionName is missing in settings. SeedHeatRecoverySources cannot initialize.");
                throw new InvalidOperationException("MongoDB HeatRecoverySourcesCollectionName is not configured. Please check appsettings.json or environment variables.");
            }

            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(awsDocDbSettings.Value.DatabaseName);
                _heatRecoverySourcesCollection = mongoDatabase.GetCollection<HeatRecovery>(awsDocDbSettings.Value.HeatRecoverySourcesCollectionName);

                _logger.LogInformation("SeedHeatRecoverySources initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    awsDocDbSettings.Value.DatabaseName, _heatRecoverySourcesCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to Db for HeatRecoverySources. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("HeatRecoverySources failed to connect to MongoDB.", ex);
            }
        }

        public async Task RunAsync()
        {
            var count = await _heatRecoverySourcesCollection.CountDocumentsAsync(FilterDefinition<HeatRecovery>.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Seeding HeatRecoverySources collection...");

                var seedData = new List<HeatRecovery>
                {
                    new HeatRecovery
                    {
                        HeatRecoveryId = 1,
                        Ident = "waste",
                        HeatRecoverySource = "Waste combustion"
                    },
                    new HeatRecovery
                    {
                        HeatRecoveryId = 2,
                        Ident = "highgrade",
                        HeatRecoverySource = "Other high grade processes"
                    },
                    new HeatRecovery
                    {
                        HeatRecoveryId = 3,
                        Ident = "geothermal",
                        HeatRecoverySource = "Geothermal or other natural sources"
                    },

                };

                await _heatRecoverySourcesCollection.InsertManyAsync(seedData);

                _logger.LogInformation("Seeding completed successfully.");
            }
            else
            {
                _logger.LogInformation("HeatRecoverySources collection already contains data. Skipping seeding.");
            }
        }
    }

}
