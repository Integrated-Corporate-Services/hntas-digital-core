using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class HeatNetworkService : IHeatNetworkService
    {
        private readonly IMongoCollection<HeatNetwork> _hnCollection;
        private readonly ILogger<HeatNetworkService> _logger;
        private readonly IAuditService _auditService;

        public HeatNetworkService(IOptions<AWSDocDbSettings> dbSettings,
            IMongoDatabase mongoDatabase,
            ILogger<HeatNetworkService> logger,
            IAuditService auditService)
        {
            _hnCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
            _logger = logger;
            _auditService = auditService;
            _logger.LogInformation("HeatNetworkService initialized via Dependency Injection.");
        }

        public async Task CreateAsync(HeatNetwork newHeatNetwork)
        {
            await _hnCollection.InsertOneAsync(newHeatNetwork);

            var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLE");
            if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                isRegistrationEnabledString.ToLower() == "true")
            {
                // Audit Code Here
                await _auditService.SaveAuditAsync<HeatNetwork>(
                    eventName: HeatNetworkEvents.Registered,
                    actorId: newHeatNetwork.CreatedBy,
                    entityId: newHeatNetwork.HnId,
                    oldState: null,
                    newState: newHeatNetwork
                );
            }

            _logger.LogInformation("New heat network initially registered...");
        }


        public async Task<List<HeatNetwork>> GetAsync()
        {
            return await _hnCollection.Find(_ => true).ToListAsync();
        }

        public async Task<HeatNetwork> GetByHnIdAsync(string hnId)
        {
            return await _hnCollection.Find(hn => hn.HnId == hnId).FirstOrDefaultAsync();
        }

        public async Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> hnIds)
        {
            var filter = Builders<HeatNetwork>.Filter.In(hn => hn.HnId, hnIds);
            return await _hnCollection.Find(filter).ToListAsync();
        }

        public async Task<List<HeatNetwork>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            // GOV.UK/ISO 8601 standard: Ensure we cover the full 'to' day
            // This sets toDate to 23:59:59 of that day
            var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);

            var filter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Gte(x => x.CreatedAt, fromDate.Date),
                Builders<HeatNetwork>.Filter.Lte(x => x.CreatedAt, endOfDay)
            );

            return await _hnCollection
                .Find(filter)
                .SortByDescending(x => x.CreatedAt) // Standard practice: show newest first
                .ToListAsync();
        }


        public async Task<HeatNetworkExternalResponse> GetDetailsByHnIdAsync(string hnId)
        {
            var matchStage = new BsonDocument("$match", new BsonDocument("hnId", hnId));

            using (var cursor = GetHeatNetworkDetailsPipeline(matchStage))
            {
                return await cursor.FirstOrDefaultAsync();
            }
        }

        public async Task<List<HeatNetworkExternalResponse>> GetDetailsAsync()
        {
            // An empty BsonDocument acts as a "match all" filter
            var matchStage = new BsonDocument("$match", new BsonDocument());

            using (var cursor = GetHeatNetworkDetailsPipeline(matchStage))
            {
                return await cursor.ToListAsync();
            }
        }

        public async Task<List<HeatNetworkExternalResponse>> GetDetailsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var endOfDay = toDate.Date.AddDays(1).AddTicks(-1);
            var matchStage = new BsonDocument("$match", new BsonDocument("createdAt",
                new BsonDocument
                {
                    { "$gte", fromDate.Date },
                    { "$lte", endOfDay }
                }));

            using (var cursor = GetHeatNetworkDetailsPipeline(matchStage))
            {
                return await cursor.ToListAsync();
            }
        }

        private IAsyncCursor<HeatNetworkExternalResponse> GetHeatNetworkDetailsPipeline(BsonDocument matchStage)
        {
            var pipeline = new List<BsonDocument>
            {
                // 1. Initial Filter
                matchStage,

                // 2. Lookup Organisation (linked via hnId in the hnIds array)
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Organisations" },
                    { "localField", "hnId" },
                    { "foreignField", "hnIds" },
                    { "as", "rpDocs" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$rpDocs" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                // 3. Lookup User (using rpUserId from the Organisation document)
                // This is where we get the emailId from
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Users" },
                    { "localField", "rpDocs.rpUserId" },
                    { "foreignField", "_id" },
                    { "as", "userDocs" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$userDocs" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                // 4. Final Projection
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", new BsonDocument("$toString", "$_id") },
                    { "hnId", 1 },
                    { "hnName", "$name" },
                    { "registrationSource", 1 },
                    { "pathway", 1 },
                    { "createdAt", 1 },
                    { "createdBy", 1 },

                    { "soa", new BsonDocument("status",
                        new BsonDocument("$ifNull", new BsonArray { "$soa.status", "" }))
                    },

                    { "energyCentre", new BsonDocument
                        {
                            { "latitude", new BsonDocument("$toString", "$ecDetails.latitude") },
                            { "longitude", new BsonDocument("$toString", "$ecDetails.longitude") },
                            { "address", "$address" }
                        }
                    },

                    // Map RP Details (using data from both the Org join and the User join)
                    { "rpDetails", new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$not", "$rpDocs"),
                            BsonNull.Value,
                            new BsonDocument
                            {
                                { "orgId", "$rpDocs.orgId" },
                                { "orgName", "$rpDocs.name" },
                                { "emailId", "$userDocs.emailId" }, // Pulled from the User join
                                { "orgAddress", "$rpDocs.registeredAddress" }
                            }
                        })
                    }
                })
            };

            return _hnCollection.Aggregate<HeatNetworkExternalResponse>(pipeline);
        }
    }
}
