using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HNTAS.Core.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Organisation> _organisationsCollection;
        private readonly IMongoCollection<HeatNetwork> _heatNetworksCollection;
        private readonly ILogger<UserService> _logger;

        public UserService(IOptions<AWSDocDbSettings> dbSettings, ILogger<UserService> logger)
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
        }

        public async Task<List<User>> GetAsync() =>
            await _usersCollection.Find(FilterDefinition<User>.Empty).ToListAsync();

        public async Task<User?> GetByIdAsync(string id) =>
            await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetByUserOneLoginIdAsync(string oneLoginId) =>
            await _usersCollection.Find(u => u.OneLoginId == oneLoginId).FirstOrDefaultAsync();

        public async Task CreateAsync(User newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateAsync(string id, User updatedUser) =>
            await _usersCollection.ReplaceOneAsync(u => u.Id == id, updatedUser);

        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(u => u.Id == id);

        public async Task<List<User>> GetRegisteredUsers(List<string> invitedEmails) =>
             await _usersCollection.Find(u => invitedEmails.Contains(u.EmailId)).ToListAsync();

        public async Task<List<User>> GetAssessorsByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.HnRoleMappings,
                mapping => mapping.HnId == hnId && mapping.Role == ContributorRole.Assessor
            );

            return await _usersCollection.Find(filter).ToListAsync();
        }


        public async Task<User?> GetResponsiblePersonByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.AnyEq(u => u.HnIds, hnId),
                Builders<User>.Filter.AnyEq(u => u.Roles, UserRole.RegulatoryContact)
            );

            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }


        public async Task<List<User>> GetContributorsByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.HnRoleMappings,
                mapping => mapping.HnId == hnId
            );

            return await _usersCollection.Find(filter).ToListAsync();
        }


        public async Task<UserDetailsResponse> GetUserWithDetailsAsync(string userId)
        {
            var userObjectId = ObjectId.Parse(userId);

            var pipeline = new[]
            {
            // Match the user by _id
            new BsonDocument("$match", new BsonDocument("_id", userObjectId)),

            // Lookup organisation using custom OrgId (string)
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "Organisations" },
                { "localField", "orgId" },           // string in Users
                { "foreignField", "orgId" },         // string in Organisations
                { "as", "organisationDetails" }
            }),

            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$organisationDetails" },
                { "preserveNullAndEmptyArrays", true }
            }),

            // Lookup heat networks using hn_id (string)
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "HeatNetworks" },
                { "localField", "hnIds" },           // array of strings in Users
                { "foreignField", "hnId" },         // string in HeatNetworks
                { "as", "heatNetworkDetails" }
            }),

            // Final projection into DTO shape
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", new BsonDocument("$toString", "$_id") },
                { "oneloginId", 1 },
                { "firstName", 1 },
                { "lastName", 1 },
                { "emailId", 1 },
                { "jobTitle", 1 },
                { "preferredContactType", 1 },
                { "landlineNumber", 1 },
                { "mobileNumber", new BsonDocument("$ifNull", new BsonArray { "$mobileNumber", BsonNull.Value }) },
                { "roles", 1 },
                { "status", 1 },

                // Null-safe organisation projection
                { "organisation", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$or", new BsonArray {
                            new BsonDocument("$eq", new BsonArray { "$organisationDetails", BsonNull.Value }),
                            new BsonDocument("$not", "$organisationDetails")
                        }) },
                        { "then", BsonNull.Value },
                        { "else", new BsonDocument
                            {
                                { "orgId", "$organisationDetails.orgId" },
                                { "name", "$organisationDetails.name" },
                                { "companiesHouseNumber", "$organisationDetails.companiesHouseNumber" },
                                { "type", "$organisationDetails.type" },
                                { "registeredAddress", "$organisationDetails.registeredAddress" }
                            }
                        }
                    })
                },

                // Heat networks projection into neatNetworks
                { "neatNetworks", new BsonDocument("$map", new BsonDocument
                    {
                        { "input", "$heatNetworkDetails" },
                        { "as", "hn" },
                        { "in", new BsonDocument
                            {
                                { "hnId", "$$hn.hnId" },
                                { "name", "$$hn.name" },
                                { "location", "$$hn.location" }
                            }
                        }
                    })
                }
            })
        };

            var result = await _usersCollection
                .Aggregate<UserDetailsResponse>(pipeline)
                .FirstOrDefaultAsync();

            return result;
        }
    }
}
