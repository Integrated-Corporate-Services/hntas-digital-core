using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.DataMigrations
{
    [ExcludeFromCodeCoverage]
    public class SeedFuelTypes : IDataMigration
    {
        private readonly AWSDocDbSettings _awsDocDbSettings;
        private readonly IMongoCollection<FuelType> _fuelTypesCollection;
        private readonly ILogger<SeedFuelTypes> _logger;

        public SeedFuelTypes(
           IOptions<AWSDocDbSettings> awsDocDbSettings,
           ILogger<SeedFuelTypes> logger)
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
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. SeedFuelTypes cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(awsDocDbSettings.Value.FuelTypesCollectionName))
            {
                _logger.LogCritical("MongoDB FuelTypesCollectionName is missing in settings. SeedFuelTypes cannot initialize.");
                throw new InvalidOperationException("MongoDB FuelTypesCollectionName is not configured. Please check appsettings.json or environment variables.");
            }

            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(awsDocDbSettings.Value.DatabaseName);
                _fuelTypesCollection = mongoDatabase.GetCollection<FuelType>(awsDocDbSettings.Value.FuelTypesCollectionName);

                _logger.LogInformation("SeedFuelTypes initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    awsDocDbSettings.Value.DatabaseName, _fuelTypesCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to Db for FuelTypes. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("FuelTypes failed to connect to MongoDB.", ex);
            }
        }

        public async Task RunAsync()
        {
            var count = await _fuelTypesCollection.CountDocumentsAsync(FilterDefinition<FuelType>.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Seeding FuelTypes collection...");

                var seedData = new List<FuelType>
                {
                    new FuelType
                    {                        
                        FuelId = 24,
                        Type = "LPG"
                    },
                    new FuelType
                    {
                        FuelId = 25,
                        Type = "Natural Gas"
                    },
                    new FuelType
                    {
                        FuelId = 26,
                        Type = "Biomethane"
                    },
                    new FuelType
                    {
                        FuelId = 27,
                        Type = "Biomass"
                    },
                    new FuelType
                    {
                        FuelId = 28,
                        Type = "Electricity"
                    },
                    new FuelType
                    {
                        FuelId = 29,
                        Type = "Biofuel"
                    },
                    new FuelType
                    {
                        FuelId = 30,
                        Type = "Burning Oil"
                    },
                };

                await _fuelTypesCollection.InsertManyAsync(seedData);

                _logger.LogInformation("Seeding completed successfully.");
            }
            else
            {
                _logger.LogInformation("FuelTypes collection already contains data. Skipping seeding.");
            }
        }
    }

}
