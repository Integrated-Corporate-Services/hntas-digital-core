using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;

namespace HNTAS.Core.Api.Services
{
    public interface IHNDataImportExportService
    {
        Task<List<HeatNetworkExportRow>> GetAllHeatNetworkRowsAsync();
    }

    public class HeatNetworkExportRow
    {
        public string HnId { get; set; } = string.Empty;
        public string HnName { get; set; } = string.Empty;
        public string HnLocation { get; set; } = string.Empty;
        public string OrganisationId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string UserEmailId { get; set; } = string.Empty;
    }

    public class HNDataImportExportService : IHNDataImportExportService
    {
        private readonly ILogger<HNDataImportExportService> _logger;
        private readonly IMongoCollection<Organisation> _orgCollection;
        private readonly IMongoCollection<BsonDocument> _heatNetworkCollection;
        private readonly IMongoCollection<BsonDocument> _usersCollection;

        public HNDataImportExportService(
            IMongoDatabase mongoDatabase,
            IOptions<AWSDocDbSettings> dbSettings,
            ILogger<HNDataImportExportService> logger)
        {
            _logger = logger;
            _orgCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
            // Use BsonDocument for collections accessed in aggregation to avoid strict typing projection issues
            _heatNetworkCollection = mongoDatabase.GetCollection<BsonDocument>(dbSettings.Value.HeatNetworksCollectionName);
            _usersCollection = mongoDatabase.GetCollection<BsonDocument>(dbSettings.Value.UsersCollectionName);
            _logger.LogInformation("HeatNetworkExportService initialized via Dependency Injection.");
        }

        public async Task<List<HeatNetworkExportRow>> GetAllHeatNetworkRowsAsync()
        {
            // Pipeline:
            // 1. Unwind organisation.hnIds so each doc represents one hnId for an organisation
            // 2. Lookup matching HeatNetwork document by hnId
            // 3. Unwind heat network details (skip organisations with no matching heat network)
            // 4. Lookup users with users.orgId == organisation.orgId
            // 5. Unwind users (preserve null to still emit rows when no users exist)
            // 6. Project flat fields for export
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$hnIds" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", _heatNetworkCollection.CollectionNamespace.CollectionName },
                    { "localField", "hnIds" },
                    { "foreignField", "hnId" },
                    { "as", "heatNetworkDetails" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$heatNetworkDetails" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", _usersCollection.CollectionNamespace.CollectionName },
                    { "localField", "orgId" },
                    { "foreignField", "orgId" },
                    { "as", "users" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$users" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "hnId", "$hnIds" },
                    { "hnName", "$heatNetworkDetails.name" },
                    { "hnLocation", "$heatNetworkDetails.location" },
                    { "organisationId", "$orgId" },
                    { "organisationName", "$name" },
                    { "userEmailId", new BsonDocument("$ifNull", new BsonArray { "$users.emailId", BsonNull.Value }) }
                })
            };

            var orgAggregate = _orgCollection.Aggregate<BsonDocument>(pipeline);

            var results = await orgAggregate.ToListAsync();

            return results.Select(doc =>
            {
                string hnId = doc.GetValue("hnId", BsonNull.Value).IsBsonNull ? string.Empty : doc["hnId"].ToString();
                string hnName = doc.GetValue("hnName", BsonNull.Value).IsBsonNull ? string.Empty : doc["hnName"].ToString();
                string hnLocation = doc.GetValue("hnLocation", BsonNull.Value).IsBsonNull ? string.Empty : doc["hnLocation"].ToString();
                string organisationId = doc.GetValue("organisationId", BsonNull.Value).IsBsonNull ? string.Empty : doc["organisationId"].ToString();
                string organisationName = doc.GetValue("organisationName", BsonNull.Value).IsBsonNull ? string.Empty : doc["organisationName"].ToString();
                string userEmailId = doc.GetValue("userEmailId", BsonNull.Value).IsBsonNull ? string.Empty : doc["userEmailId"].ToString();

                return new HeatNetworkExportRow
                {
                    HnId = hnId,
                    HnName = hnName,
                    HnLocation = hnLocation,
                    OrganisationId = organisationId,
                    OrganisationName = organisationName,
                    UserEmailId = userEmailId
                };
            }).ToList();
        }


    }
}

// PSEUDOCODE / PLAN:
// - Create interface `IHNDataImportExportService` with a single async method:
//     Task<List<HeatNetworkExportRow>> GetAllHeatNetworkRowsAsync();
// - Create DTO `HeatNetworkExportRow` with properties:
//     string HnId, string HnName, string HnLocation, string OrganisationId, string OrganisationName, string UserEmailId
// - Implement `HeatNetworkExportService`:
//   - Inject IMongoDatabase, IOptions<AWSDocDbSettings>, ILogger<HeatNetworkExportService>
//   - Obtain collections: Organisations, HeatNetworks, Users using settings
//   - Build an aggregation pipeline starting from Organisations:
//       1. $unwind "hnIds" to get one document per organisation-hnId pair
//       2. $lookup into HeatNetworks where localField "hnIds" == foreignField "hnId" -> "heatNetworkDetails"
//       3. $unwind "heatNetworkDetails" (preserveNullAndEmptyArrays: false) to only include organisations with matching HNs
//       4. $lookup into Users where localField "orgId" == foreignField "orgId" -> "users"
//       5. $unwind "users" (preserveNullAndEmptyArrays: true) to produce a row per user; if no user, row still present with null email
//       6. $project the flat fields: hnId, hnName, hnLocation, organisationId, organisationName, userEmailId
//   - Execute aggregation and map results to `HeatNetworkExportRow` list
// - Keep mapping safe for nulls; map missing user email to empty string
//
// Note: This service returns a flat list suitable for CSV export (one record per row).