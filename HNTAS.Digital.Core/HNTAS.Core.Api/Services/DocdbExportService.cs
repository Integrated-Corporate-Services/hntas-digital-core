using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime;

namespace HNTAS.Core.Api.Services
{
    public class DocdbExportRow
    {
        public string HnId { get; set; } = "";
        public string HeatNetworkName { get; set; } = "";
        public string Location { get; set; } = "";
        public string OrgId { get; set; } = "";
        public string EmailId { get; set; } = "";
        public string OrganisationName { get; set; } = "";
    }

    public class DocdbExportService : IDocdbExportService
    {
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<HeatNetwork> _heatNetworksCollection;
        private readonly IMongoCollection<Organisation> _organisationsCollection;
        private readonly ILogger<UserService> _logger;


        public string Elem<T>(string property)
        {
            var map = MongoDB.Bson.Serialization.BsonClassMap.LookupClassMap(typeof(T));
            var mm = map.GetMemberMap(property);
            return mm?.ElementName ?? property; // fallback to property if no map
        }


        public DocdbExportService(IOptions<AWSDocDbSettings> dbSettings, ILogger<UserService> logger)
        {
            _logger = logger;

            var connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable.");
            }

            _logger.LogInformation("Initializing UserService with connection string: {ConnectionString}", connectionString);

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
            _usersCollection = mongoDatabase.GetCollection<User>(dbSettings.Value.UsersCollectionName);
            _heatNetworksCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
            _organisationsCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
        }



        public async Task<List<DocdbExportRow>> GetFlattenedHeatNetworkUserOrgAsync()
        {
            var usersCollName = _usersCollection.CollectionNamespace.CollectionName;
            var orgsCollName = _organisationsCollection.CollectionNamespace.CollectionName;
            var hnsCollName = _heatNetworksCollection.CollectionNamespace.CollectionName;

            // Resolve stored BSON field names from your POCOs
            // --- HeatNetwork
            var hn_HnId = Elem<HeatNetwork>(nameof(HeatNetwork.HnId));
            var hn_Name = Elem<HeatNetwork>(nameof(HeatNetwork.Name));
            var hn_Location = Elem<HeatNetwork>(nameof(HeatNetwork.Location));

            // --- User
            var user_HnIds = Elem<User>(nameof(User.HnIds));      // array
            var user_EmailId = Elem<User>(nameof(User.EmailId));
            var user_OrgId = Elem<User>(nameof(User.OrgId));
            var user_Roles = Elem<User>(nameof(User.Roles));       // array of strings

            // --- Organisation
            var org_OrgId = Elem<Organisation>(nameof(Organisation.OrgId));
            var org_Name = Elem<Organisation>(nameof(Organisation.Name));

            var pipeline = new[]
            {
                // 1) Join Users where Users.HnIds contains HeatNetwork.HnId
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", usersCollName },
                    { "localField", hn_HnId },       // stored name in HeatNetwork
                    { "foreignField", user_HnIds },  // stored array name in Users
                    { "as", "users" }
                }),

                // 2) One row per matched user
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$users" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                // 2a) Keep only users whose Roles contain "ResponsiblePerson"
                // Equality on an array field in Mongo/DocDB matches when any element equals the value.
                new BsonDocument("$match", new BsonDocument
                {
                    { $"users.{user_Roles}", "ResponsiblePerson" }
                }),

                // 3) Join Organisation on user's OrgId
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", orgsCollName },
                    { "localField", $"users.{user_OrgId}" }, // users.<stored OrgId>
                    { "foreignField", org_OrgId },           // organisation <stored OrgId>
                    { "as", "org" }
                }),

                // 4) Expect 0/1 org; keep even if missing
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$org" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                // 5) Project into your DocdbExportRow DTO using stored field names
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { nameof(DocdbExportRow.HnId),             $"${hn_HnId}" },
                    { nameof(DocdbExportRow.HeatNetworkName),  $"${hn_Name}" },
                    { nameof(DocdbExportRow.Location),         $"${hn_Location}" },
                    { nameof(DocdbExportRow.OrgId),            $"$users.{user_OrgId}" },
                    { nameof(DocdbExportRow.EmailId),          $"$users.{user_EmailId}" },
                    { nameof(DocdbExportRow.OrganisationName), $"$org.{org_Name}" }
                })
            };

            var rows = await _heatNetworksCollection
                .Aggregate<DocdbExportRow>(pipeline)
                .ToListAsync();

            _logger.LogInformation("Flattened rows (ResponsiblePerson only): {Count}", rows.Count);
            if (rows.Count > 0)
            {
                var r = rows[0];
                _logger.LogInformation("Sample row -> HnId={HnId}, Name={Name}, Loc={Loc}, Email={Email}, OrgId={OrgId}, OrgName={OrgName}",
                    r.HnId, r.HeatNetworkName, r.Location, r.EmailId, r.OrgId, r.OrganisationName);
            }
            return rows;
        }
    }
}
