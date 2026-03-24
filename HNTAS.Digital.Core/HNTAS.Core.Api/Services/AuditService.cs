using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly IMongoDatabase _mongoDatabase;

        public AuditService(ILogger<AuditService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _mongoDatabase = mongoDatabase;
            _logger.LogInformation("AuditService (Per-Collection Pattern) initialized.");
        }

        public async Task SaveAuditAsync<T>(
            string entryType,
            string actorId,
            string entityId,
            T? oldState,
            T? newState,            
            string elementName,
            string phase,
            string stage,
            string? changeNote = null
            )
        {
            try
            {
                // Resolve collection name: e.g., "Audit_HeatNetworks" or "Audit_Assessors"
                var collectionName = $"Audit_{typeof(T).Name}s";
                var collection = _mongoDatabase.GetCollection<AuditEntry<T>>(collectionName);

                var entry = new AuditEntry<T>
                {
                    EntryType = entryType,
                    EntityId = entityId,
                    UserId = actorId,
                    Before = oldState,
                    After = newState,
                    ChangeNote = changeNote,
                    Timestamp = DateTime.UtcNow,                    
                    ElementName = elementName,
                    Phase = phase,
                    Stage = stage
                };

                await collection.InsertOneAsync(entry);
                _logger.LogInformation("Audit event {EntryType} recorded in {Collection}", entryType, collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record audit event {EntryType} for entity {EntityId}", entryType, entityId);
                // In a POC, we usually don't want an audit failure to crash the main business flow,
                // but you may choose to re-throw based on compliance needs.
            }
        }

        public async Task<List<AuditEntry<T>>> GetHistoryAsync<T>(string entityId)
        {
            var collectionName = $"Audit_{typeof(T).Name}s";
            var collection = _mongoDatabase.GetCollection<AuditEntry<T>>(collectionName);

            // Return history sorted by newest first
            return await collection
                .Find(x => x.EntityId == entityId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
        }


        public async Task<List<AuditLogResponse>> GetAuditHistoryAsync<T>(string entityId)
        {
            // 1. Determine collection names
            var collectionName = $"Audit_{typeof(T).Name}s";
            var auditCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);

            // London Time Zone for UK compliance
            var londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            // 2. Build Aggregation Pipeline
            var pipeline = auditCollection.Aggregate()
                .Match(new BsonDocument("entityId", entityId))
                // Join with Users collection
                .Lookup("Users", "userId", "_id", "joinedUser")
                // Flatten the joinedUser array (left outer join)
                .Unwind("joinedUser", new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Sort(Builders<BsonDocument>.Sort.Descending("timestamp"));

            var results = await pipeline.ToListAsync();

            // 3. Map to DTO
            return results.Select(doc =>
            {
                var userDoc = doc.Contains("joinedUser") && !doc["joinedUser"].IsBsonNull
                              ? doc["joinedUser"].AsBsonDocument
                              : null;

                string roleDescription = "N/A";

                if (userDoc != null)
                {
                    // PRIORITY 1: Check specific Heat Network role mappings (per GetHeatNetworksForUser logic)
                    bool foundSpecificMapping = false;
                    if (userDoc.Contains("hnRoleMappings") && userDoc["hnRoleMappings"].IsBsonArray)
                    {
                        var mapping = userDoc["hnRoleMappings"].AsBsonArray
                            .Select(m => m.AsBsonDocument)
                            .FirstOrDefault(m => m.Contains("hnId") && m["hnId"].AsString == entityId);

                        if (mapping != null && Enum.TryParse(mapping["role"].AsString, out ContributorRole hnRole))
                        {
                            roleDescription = hnRole.GetDescription();
                            foundSpecificMapping = true;
                        }
                    }

                    // PRIORITY 2: If no specific mapping, check for "Full Access" Org roles (RP or Coordinator)
                    if (!foundSpecificMapping && userDoc.Contains("roles") && userDoc["roles"].IsBsonArray)
                    {
                        var globalRoles = userDoc["roles"].AsBsonArray.Select(r => r.AsString).ToList();

                        // Check for Responsible Person first, then Coordinator
                        if (globalRoles.Contains(UserRole.ResponsiblePerson.ToString()))
                        {
                            roleDescription = UserRole.ResponsiblePerson.GetDescription();
                        }
                        else if (globalRoles.Contains(UserRole.Coordinator.ToString()))
                        {
                            roleDescription = UserRole.Coordinator.GetDescription();
                        }
                    }
                }

                return new AuditLogResponse
                {
                    EntryType = doc.Contains("entryType") ? doc["entryType"].AsString : "Unknown",
                    UserName = userDoc != null
                        ? $"{userDoc.GetValue("firstName", string.Empty)} {userDoc.GetValue("lastName", string.Empty)}".Trim()
                        : "System Process",
                    Role = roleDescription,
                    Timestamp = TimeZoneInfo.ConvertTimeFromUtc(doc["timestamp"].ToUniversalTime(), londonTimeZone)
                                            .ToString("dd MMM yyyy HH:mm:ss"),
                    ElementName = doc.Contains("elementName") ? doc["elementName"].AsString : "Unknown",
                    Phase = doc.Contains("phase") ? doc["phase"].AsString : "Unknown",
                    Stage = doc.Contains("stage") ? doc["stage"].AsString : "Unknown"
                };
            }).ToList();
        }
    }
}
