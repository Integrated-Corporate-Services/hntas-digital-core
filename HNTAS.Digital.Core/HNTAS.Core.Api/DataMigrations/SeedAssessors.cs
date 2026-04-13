using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.DataMigrations
{
    public class SeedAssessors : IDataMigration
    {
        private readonly AWSDocDbSettings _awsDocDbSettings;
        private readonly IMongoCollection<Assessor> _assessorCollection;
        private readonly ILogger<SeedAssessors> _logger;

        public SeedAssessors(
           IOptions<AWSDocDbSettings> awsDocDbSettings,
           ILogger<SeedAssessors> logger)
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
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. CounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(awsDocDbSettings.Value.CountersCollectionName))
            {
                _logger.LogCritical("MongoDB OrgCountersCollectionName is missing in settings. CounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB OrgCountersCollectionName is not configured. Please check appsettings.json or environment variables.");
            }

            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(awsDocDbSettings.Value.DatabaseName);
                _assessorCollection = mongoDatabase.GetCollection<Assessor>(awsDocDbSettings.Value.AssessorsCollectionName);

                _logger.LogInformation("SeedAssessors initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    awsDocDbSettings.Value.DatabaseName, _assessorCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to Db for CounterService. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("CounterService failed to connect to MongoDB.", ex);
            }
        }

        public async Task RunAsync()
        {
            var count = await _assessorCollection.CountDocumentsAsync(FilterDefinition<Assessor>.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Seeding Assessor collection...");

                var seedData = new List<Assessor>
                {
                    new Assessor
                    {                        
                        FirstName = "Assessor1",
                        LastName = "Hntas",
                        Email = "assessor1_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor1 Hntas (assessor1_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor2",
                        LastName = "Hntas",
                        Email = "assessor2_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor2 Hntas (assessor2_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor3",
                        LastName = "Hntas",
                        Email = "assessor3_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor3 Hntas (assessor3_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor4",
                        LastName = "Hntas",
                        Email = "assessor4_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor4 Hntas (assessor4_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor5",
                        LastName = "Hntas",
                        Email = "assessor5_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor5 Hntas (assessor5_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor6",
                        LastName = "Hntas",
                        Email = "assessor6_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor6 Hntas (assessor6_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor7",
                        LastName = "Hntas",
                        Email = "assessor7_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor7 Hntas (assessor7_hntas@mailinator.com)"
                    },
                    new Assessor
                    {
                        FirstName = "Assessor8",
                        LastName = "Hntas",
                        Email = "assessor8_hntas@mailinator.com",
                        Status = UserStatus.Active,
                        FullNameWithEmail = "Assessor8 Hntas (assessor8_hntas@mailinator.com)"
                    },
                };

                await _assessorCollection.InsertManyAsync(seedData);

                _logger.LogInformation("Seeding completed successfully.");
            }
            else
            {
                _logger.LogInformation("Assessor collection already contains data. Skipping seeding.");
            }
        }
    }

}
