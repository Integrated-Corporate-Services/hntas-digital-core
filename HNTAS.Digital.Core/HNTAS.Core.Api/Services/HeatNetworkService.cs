using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.External;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.AssignedAssessor;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

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

        public async Task CreateAsync(HeatNetwork newHeatNetwork, bool isNewHeatNetwork = false)
        {
            await _hnCollection.InsertOneAsync(newHeatNetwork);

            var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
            if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                isRegistrationEnabledString.ToLower() == "true" && isNewHeatNetwork)
            {
                // Audit Code Here               
                await _auditService.SaveAuditAsync<HeatNetwork>(
                    entryType: HeatNetworkEvents.Registered,
                    actorId: newHeatNetwork.CreatedBy,
                    entityId: newHeatNetwork.HnId!,
                    oldState: null,
                    newState: newHeatNetwork,
                    elementName: "All Elements",
                    phase: newHeatNetwork.Phase,
                    stage: HeatNetworkHelper.GetStageFromPhase(newHeatNetwork.Phase)
                );                
            }

            _logger.LogInformation("New heat network initially registered...");
        }


        public async Task UpdateAsync(string hnId, HeatNetwork updatedHn) =>
            await _hnCollection.ReplaceOneAsync(hn => hn.HnId == hnId, updatedHn);

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

        public async Task UpdateMeteringAndMonitoringStrategyAsync(string hnId, NetworkDetailsDocument document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(
                    hn => hn.MeteringAndMonitoringStrategy!.Documents,
                    doc => doc.S3Key != null
                ));

            var update = Builders<HeatNetwork>.Update
                .Set("meteringAndMonitoringStrategy.documents.$", document)
                .Set(hn => hn.MeteringAndMonitoringStrategy!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.MeteringAndMonitoringStrategy.UpdatedBy, document.UploadedBy)
                .Set(hn => hn.MeteringAndMonitoringStrategy.Status, NetworkDetailsStatus.Complete);

            var result = await _hnCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.MeteringAndMonitoringStrategy, null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set(
                    hn => hn.MeteringAndMonitoringStrategy,
                    new MeteringAndMonitoringStrategy
                    {
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = document.UploadedBy,
                        Status = NetworkDetailsStatus.Complete,
                        Documents = new List<NetworkDetailsUploadedDocument>()
                    }
                );

                await _hnCollection.UpdateOneAsync(initFilter, initUpdate);

                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("meteringAndMonitoringStrategy.documents", document)
                    .Set(hn => hn.MeteringAndMonitoringStrategy!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.MeteringAndMonitoringStrategy.UpdatedBy, document.UploadedBy)
                    .Set(hn => hn.MeteringAndMonitoringStrategy.Status, NetworkDetailsStatus.Complete);

                await _hnCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        }

        public async Task UpdateAssessmentPlanAsync(string hnId, NetworkDetailsDocument document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(
                    hn => hn.AssessmentPlan!.Documents,
                    doc => doc.S3Key != null
                ));

            var update = Builders<HeatNetwork>.Update
                .Set("assessmentPlan.documents.$", document)
                .Set(hn => hn.AssessmentPlan!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.AssessmentPlan.UpdatedBy, document.UploadedBy)
                .Set(hn => hn.AssessmentPlan.Status, NetworkDetailsStatus.Complete);

            var result = await _hnCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.AssessmentPlan, null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set(
                    hn => hn.AssessmentPlan,
                    new AssessmentPlan
                    {
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = document.UploadedBy,
                        Status = NetworkDetailsStatus.Complete,
                        Documents = new List<NetworkDetailsUploadedDocument>()
                    }
                );

                await _hnCollection.UpdateOneAsync(initFilter, initUpdate);

                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("assessmentPlan.documents", document)
                    .Set(hn => hn.AssessmentPlan!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.AssessmentPlan.UpdatedBy, document.UploadedBy)
                    .Set(hn => hn.AssessmentPlan.Status, NetworkDetailsStatus.Complete);

                await _hnCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        }

        public async Task UpdateDesignConstructionLogAsync(string hnId, NetworkDetailsDocument document)
        {
            var updateFilter = Builders<HeatNetwork>.Filter.And(
                Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                Builders<HeatNetwork>.Filter.ElemMatch(
                    hn => hn.DesignConstructionLog!.Documents,
                    doc => doc.S3Key != null
                ));

            var update = Builders<HeatNetwork>.Update
                .Set("designConstructionLog.documents.$", document)
                .Set(hn => hn.DesignConstructionLog!.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.DesignConstructionLog.UpdatedBy, document.UploadedBy)
                .Set(hn => hn.DesignConstructionLog.Status, NetworkDetailsStatus.Complete);

            var result = await _hnCollection.UpdateOneAsync(updateFilter, update);

            if (result.ModifiedCount == 0)
            {
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.DesignConstructionLog, null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set(
                    hn => hn.DesignConstructionLog,
                    new DesignConstructionLog
                    {
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = document.UploadedBy,
                        Status = NetworkDetailsStatus.Complete,
                        Documents = new List<NetworkDetailsUploadedDocument>()
                    }
                );

                await _hnCollection.UpdateOneAsync(initFilter, initUpdate);

                var insertFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                var insertUpdate = Builders<HeatNetwork>.Update
                    .Push("designConstructionLog.documents", document)
                    .Set(hn => hn.DesignConstructionLog!.UpdatedAt, DateTime.UtcNow)
                    .Set(hn => hn.DesignConstructionLog.UpdatedBy, document.UploadedBy)
                    .Set(hn => hn.DesignConstructionLog.Status, NetworkDetailsStatus.Complete);

                await _hnCollection.UpdateOneAsync(insertFilter, insertUpdate);
            }
        }

        public async Task<AssignedAssessorResponse> GetAssignedAssessors(AssignedAssessorRequest request)
        {
            // Build filter for elements with assigned assessors
            var filter = Builders<HeatNetwork>.Filter.ElemMatch(
                hn => hn.NetworkElements!.Elements,
                element => element.SoaStages != null && element.SoaStages.Any(soa => soa.Assessor != null)
            );

            var heatNetworks = await _hnCollection.Find(filter).ToListAsync();

            // Flatten heat networks into individual assessor assignments
            var assignedAssessors = heatNetworks
                .SelectMany(hn =>
                    (hn.NetworkElements?.Elements ?? Enumerable.Empty<Element>())
                        .Where(element => element.SoaStages?.FirstOrDefault()?.Assessor != null)
                        .Select(element =>
                        {
                            var soaStage = element.SoaStages!.First();
                            var assessor = soaStage.Assessor!;

                            return new AssignedAssessor
                            {
                                Name = $"{assessor.FirstName} {assessor.LastName}".Trim(),
                                Email = assessor.Email,
                                HeatNetworkName = hn.Name,                                
                                Status = assessor.Status,
                                AssessorUpdatedAt = soaStage.AssessorUpdatedAt
                            };
                        })
                )
                .ToList();

            // Group by HeatNetwork and Assessor, then aggregate element assignments
            var groupedAssessors = assignedAssessors
                .GroupBy(a => new { a.HeatNetworkName, a.Email })
                .Select(g => new AssignedAssessor
                {
                    HeatNetworkName = g.Key.HeatNetworkName,
                    Email = g.Key.Email,
                    Name = g.First().Name,
                    Status = g.First().Status,
                    ElementsAssignedList = g.Select(a => a.ElementsAssigned).ToList()!,
                    ElementsAssigned = string.Join(", ", g.Select(a => a.ElementsAssigned)),
                    AssessorUpdatedAt = g.Max(a => a.AssessorUpdatedAt)
                })
                .AsQueryable();

            // Apply sorting
            groupedAssessors = ApplySorting(groupedAssessors, request.SortBy, request.SortDirection);

            var totalCount = groupedAssessors.Count();

            // Apply pagination
            var paginatedResults = groupedAssessors
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new AssignedAssessorResponse
            {
                Items = paginatedResults,
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        private static IQueryable<AssignedAssessor> ApplySorting(
            IQueryable<AssignedAssessor> query,
            string? sortBy,
            string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLower()) switch
            {
                "name" => isDescending
                    ? query.OrderByDescending(a => a.Name)
                    : query.OrderBy(a => a.Name),
                "email" => isDescending
                    ? query.OrderByDescending(a => a.Email)
                    : query.OrderBy(a => a.Email),
                "heatnetworkname" => isDescending
                    ? query.OrderByDescending(a => a.HeatNetworkName)
                    : query.OrderBy(a => a.HeatNetworkName),
                "elementsassigned" => isDescending
                    ? query.OrderByDescending(a => a.ElementsAssigned)
                    : query.OrderBy(a => a.ElementsAssigned),
                "status" => isDescending
                    ? query.OrderByDescending(a => a.Status)
                    : query.OrderBy(a => a.Status),
                _ => isDescending
                    ? query.OrderByDescending(a => a.AssessorUpdatedAt)
                    : query.OrderBy(a => a.AssessorUpdatedAt)
            };
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
