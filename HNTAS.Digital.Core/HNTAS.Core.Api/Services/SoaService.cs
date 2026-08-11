using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class SoaService : ISoaService
    {
        private readonly IMongoCollection<HeatNetwork> _heatNetworkCollection;
        private readonly ILogger<SoaService> _logger;

        public SoaService(IOptions<AWSDocDbSettings> dbSettings, ILogger<SoaService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _heatNetworkCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
            _logger.LogInformation("SoaService initialized via Dependency Injection.");
        }        

        public async Task<Soa?> UpdateStatusAsync(string hnId, SoaStatus newStatus, string updatedBy)
        {
            var filter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

            var update = Builders<HeatNetwork>.Update
                .Set(hn => hn.Soa!.Status, newStatus)
                .Set(hn => hn.Soa.UpdatedAt, DateTime.UtcNow)
                .Set(hn => hn.Soa.UpdatedBy, updatedBy);

            await _heatNetworkCollection.UpdateOneAsync(filter, update);

            var updated = await _heatNetworkCollection.Find(filter).FirstOrDefaultAsync();
            return updated?.Soa;
        }       

        public async Task UpdateSoaStatus(string hnId, ElementTypeInShort elementType, SoaStage stage, List<SoaStatusWithCount> soaStatuses, string updatedBy, NetworkDetailsStatus elementSoaStatus)
        {
            try
            {
                // First, ensure SoaStages is initialized
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.NetworkElements!.ElementsGroup,
                        e => e.ElementType == elementType && e.SoaStages == null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set("networkElements.elementsGroup.$.soaStages", new List<SoaStages>());

                await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                // Try to update existing status for the specific stage and element
                var updateFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch<ElementGroup>("networkElements.elementsGroup",
                        new MongoDB.Bson.BsonDocument
                        {
                            { "elementType", elementType.ToString() },
                            { "soaStages.stageId", stage.ToString() }
                        })
                );

                var update = Builders<HeatNetwork>.Update
                    .Set("networkElements.elementsGroup.$[element].soaStages.$[stage].soaStatuses", soaStatuses)
                    .Set("networkElements.elementsGroup.$[element].soaStages.$[stage].soaStatusUpdatedAt", DateTime.UtcNow)
                    .Set("networkElements.elementsGroup.$[element].soaStages.$[stage].soaStatusUpdatedBy", updatedBy)
                    .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("element.elementType", elementType.ToString())),
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("stage.stageId", stage.ToString()))
                };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
                var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update, updateOptions);

                if (result.ModifiedCount == 0)
                {
                    // Stage doesn't exist - add it using array filter
                    var pushFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                    var pushUpdate = Builders<HeatNetwork>.Update
                        .Push("networkElements.elementsGroup.$[element].soaStages", new SoaStages
                        {
                            StageId = stage,
                            SoaStatuses = soaStatuses,
                            SoaStatusUpdatedAt = DateTime.UtcNow,
                            SoaStatusUpdatedBy = updatedBy                            
                        })
                        .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                    var pushArrayFilters = new[]
                    {
                        new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                            new MongoDB.Bson.BsonDocument("element.elementType", elementType.ToString()))
                    };

                    var pushOptions = new UpdateOptions { ArrayFilters = pushArrayFilters };
                    result = await _heatNetworkCollection.UpdateOneAsync(pushFilter, pushUpdate, pushOptions);

                    if (result.ModifiedCount > 0)
                    {
                        _logger.LogInformation("Added document to existing element for HN ID: {HnId}, Stage: {Stage}, Element: {elementType}", StringFormatter.Sanitize(hnId), stage, StringFormatter.Sanitize(elementType.ToString()));
                        return;
                    }
                }

                _logger.LogInformation("Updated ElementSoa document for HN ID: {HnId}, Stage: {Stage}, Element: {elementType}", StringFormatter.Sanitize(hnId), stage, StringFormatter.Sanitize(elementType.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SOA document for HN ID: {HnId}, Element: {elementType}, Stage: {Stage}", StringFormatter.Sanitize(hnId), StringFormatter.Sanitize(elementType.ToString()), stage);
                throw;
            }

        }

        public async Task UpdateSoaStatusForExistingNetwork(string hnId, ElementTypeInShort elementType, Milestone milestone, List<SoaStatusWithCountExistingNetwork> soaStatuses, string updatedBy, NetworkDetailsStatus elementSoaStatus)
        {
            try
            {
                // First, ensure SoaMilestone is initialized
                var initFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.NetworkElements!.ElementsGroup,
                        e => e.ElementType == elementType && e.SoaMilestones == null)
                );

                var initUpdate = Builders<HeatNetwork>.Update.Set("networkElements.elementsGroup.$.soaMilestones", new List<SoaMilestone>());

                await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                // Try to update existing status for the specific milestone and element
                var updateFilter = Builders<HeatNetwork>.Filter.And(
                    Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId),
                    Builders<HeatNetwork>.Filter.ElemMatch<ElementGroup>("networkElements.elementsGroup",
                        new MongoDB.Bson.BsonDocument
                        {
                            { "elementType", elementType.ToString() },
                            { "soaMilestones.milestoneId", milestone.ToString() }
                        })
                );

                var update = Builders<HeatNetwork>.Update
                    .Set("networkElements.elementsGroup.$[element].soaMilestones.$[milestone].soaStatuses", soaStatuses)
                    .Set("networkElements.elementsGroup.$[element].soaMilestones.$[milestone].soaStatusUpdatedAt", DateTime.UtcNow)
                    .Set("networkElements.elementsGroup.$[element].soaMilestones.$[milestone].soaStatusUpdatedBy", updatedBy)
                    .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                var arrayFilters = new[]
                {
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("element.elementType", elementType.ToString())),
                    new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("milestone.milestoneId", milestone.ToString()))
                };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
                var result = await _heatNetworkCollection.UpdateOneAsync(updateFilter, update, updateOptions);

                if (result.ModifiedCount == 0)
                {
                    // Milestone doesn't exist - add it using array filter
                    var pushFilter = Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, hnId);

                    var pushUpdate = Builders<HeatNetwork>.Update
                        .Push("networkElements.elementsGroup.$[element].soaMilestones", new SoaMilestone
                        {
                            MilestoneId = milestone,
                            SoaStatuses = soaStatuses,
                            SoaStatusUpdatedAt = DateTime.UtcNow,
                            SoaStatusUpdatedBy = updatedBy
                        })
                        .Set(hn => hn.NetworkElements!.ElementSoaStatus, elementSoaStatus);

                    var pushArrayFilters = new[]
                    {
                        new BsonDocumentArrayFilterDefinition<MongoDB.Bson.BsonDocument>(
                            new MongoDB.Bson.BsonDocument("element.elementType", elementType.ToString()))
                    };

                    var pushOptions = new UpdateOptions { ArrayFilters = pushArrayFilters };
                    result = await _heatNetworkCollection.UpdateOneAsync(pushFilter, pushUpdate, pushOptions);

                    if (result.ModifiedCount > 0)
                    {
                        _logger.LogInformation("Added document to existing element for existing HN ID: {HnId}, Milestone: {Milestone}, Element: {elementType}", StringFormatter.Sanitize(hnId), milestone, StringFormatter.Sanitize(elementType.ToString()));
                        return;
                    }
                }

                _logger.LogInformation("Updated ElementSoa document for existing HN ID: {HnId}, Milestone: {Milestone}, Element: {elementType}", StringFormatter.Sanitize(hnId), milestone, StringFormatter.Sanitize(elementType.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SOA document for existing HN ID: {HnId}, Element: {elementType}, Milestone: {Milestone}", StringFormatter.Sanitize(hnId), StringFormatter.Sanitize(elementType.ToString()), milestone);
                throw;
            }

        }

        public async Task<NetworkElements> UpdateAssignAssessor(ElementSoaAssignAssessorRequest request, NetworkElements networkElements, string phase,  bool initiateSoa)
        {
            var networkElementsName = new List<string>();
            try
            {                
                if (initiateSoa)
                {
                    foreach (var elementsAndAssessment in request.AssessorAssessmentForElements)
                    {
                        // First, ensure SoaStages is initialized
                        var initFilter = Builders<HeatNetwork>.Filter.And(
                            Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, request.HnId),
                            Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.NetworkElements!.ElementsGroup,
                                e => e.ElementType == elementsAndAssessment.ElementType && e.SoaStages == null)
                        );

                        var initUpdate = Builders<HeatNetwork>.Update.Set("networkElements.elements.$.soaStages", new List<SoaStages>());

                        await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                    }
                }
                else
                {
                    foreach (var networkElement in networkElements.ElementsGroup)
                    {
                        var isNetworkElementToUpdate = request.AssessorAssessmentForElements.Any(e => e.ElementType == networkElement.ElementType);
                        if (isNetworkElementToUpdate)
                        {                            
                            var stage = request.SoaStage.ToString();

                                var stageExists = networkElement.SoaStages?.Any(s => s.StageId.ToString() == stage) ?? false;
                                if (stageExists)
                                {
                                    networkElement.SoaStages?.ForEach(networkElementStage =>
                                    {
                                        if (networkElementStage.StageId.ToString() == stage)
                                        {
                                            var assessorAssessments = request.AssessorAssessmentForElements.FirstOrDefault(e => e.ElementType == networkElement.ElementType)?.AssessorAssessments;

                                            foreach (var assessorAssessment in assessorAssessments!)
                                            {
                                                // Initialize Assessors list if null (for backward compatibility with existing data)
                                                networkElementStage.Assessors ??= [];
                                                
                                                var existingAssessor = networkElementStage.Assessors.FirstOrDefault(a => a.Email == assessorAssessment.AssessorEmail);
                                                if (existingAssessor == null)
                                                {
                                                    networkElementStage.Assessors.Add(
                                                        new SoaAssessor
                                                        {
                                                            FirstName = assessorAssessment.AssessorFirstName,
                                                            LastName = assessorAssessment.AssessorLastName,
                                                            Email = assessorAssessment.AssessorEmail,
                                                            Status = UserStatus.Active,
                                                            Assessment = assessorAssessment.Assessment
                                                        });
                                                }
                                                else
                                                {
                                                    existingAssessor.Assessment = assessorAssessment.Assessment;
                                                }

                                            }
                                                                                                
                                            networkElementStage.AssessorUpdatedAt = DateTime.UtcNow;
                                            networkElementStage.AssessorUpdatedBy = request.UpdatedBy;
                                        }
                                    });
                                }
                                else
                                {
                                    networkElement.SoaStages?.Add(new SoaStages
                                    {
                                        StageId = Enum.Parse<SoaStage>(stage),                                        
                                        AssessorUpdatedAt = DateTime.UtcNow,
                                        AssessorUpdatedBy = request.UpdatedBy,
                                        Assessors = request.AssessorAssessmentForElements.FirstOrDefault(e => e.ElementType == networkElement.ElementType)?.AssessorAssessments.Select(assessorAssessment => new SoaAssessor
                                        {
                                            FirstName = assessorAssessment.AssessorFirstName,
                                            LastName = assessorAssessment.AssessorLastName,
                                            Email = assessorAssessment.AssessorEmail,
                                            Status = UserStatus.Active,
                                            Assessment = assessorAssessment.Assessment
                                        }).ToList() ?? new List<SoaAssessor>()                                        
                                    });
                                }
                        
                            _logger.LogInformation("Updated Assigned Assessor for HN ID: {HnId}, Element(s): {Element}", StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(string.Join(", ", networkElementsName)));
                        }                        
                    }
                }
                networkElements.ElementSoaStatus = NetworkDetailsStatus.InProgress;
                return networkElements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Assigned Assessor for HN ID: {HnId}, Element(s): {Element}", StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(string.Join(", ", networkElementsName)));
                throw;
            }

        }

        public async Task<NetworkElements> UpdateAssignAssessorForExistingNetwork(ElementSoaAssignAssessorRequestForExistingNetwork request, NetworkElements networkElements, string phase, bool initiateSoa)
        {
            var networkElementsName = new List<string>();
            try
            {
                if (initiateSoa)
                {
                    foreach (var elementsAndAssessment in request.AssessorAssessmentForElements)
                    {
                        // First, ensure SoaMilestone is initialized
                        var initFilter = Builders<HeatNetwork>.Filter.And(
                            Builders<HeatNetwork>.Filter.Eq(hn => hn.HnId, request.HnId),
                            Builders<HeatNetwork>.Filter.ElemMatch(hn => hn.NetworkElements!.ElementsGroup,
                                e => e.ElementType == elementsAndAssessment.ElementType && e.SoaMilestones == null)
                        );

                        var initUpdate = Builders<HeatNetwork>.Update.Set("networkElements.elements.$.soaMilestones", new List<SoaMilestone>());

                        await _heatNetworkCollection.UpdateOneAsync(initFilter, initUpdate);

                    }
                }
                else
                {
                    foreach (var networkElement in networkElements.ElementsGroup)
                    {
                        var isNetworkElementToUpdate = request.AssessorAssessmentForElements.Any(e => e.ElementType == networkElement.ElementType);
                        if (isNetworkElementToUpdate)
                        {
                            var milestone = request.Milestone.ToString();

                            var milestoneExists = networkElement.SoaMilestones?.Any(s => s.MilestoneId.ToString() == milestone) ?? false;
                            if (milestoneExists)
                            {
                                networkElement.SoaMilestones?.ForEach(networkElementMilestone =>
                                {
                                    if (networkElementMilestone.MilestoneId.ToString() == milestone)
                                    {
                                        var assessorAssessments = request.AssessorAssessmentForElements.FirstOrDefault(e => e.ElementType == networkElement.ElementType)?.AssessorAssessments;

                                        foreach (var assessorAssessment in assessorAssessments!)
                                        {
                                            // Initialize Assessors list if null (for backward compatibility with existing data)
                                            networkElementMilestone.Assessors ??= [];

                                            var existingAssessor = networkElementMilestone.Assessors.FirstOrDefault(a => a.Email == assessorAssessment.AssessorEmail);
                                            if (existingAssessor == null)
                                            {
                                                networkElementMilestone.Assessors.Add(
                                                    new SoaAssessorExistingNetwork
                                                    {
                                                        FirstName = assessorAssessment.AssessorFirstName,
                                                        LastName = assessorAssessment.AssessorLastName,
                                                        Email = assessorAssessment.AssessorEmail,
                                                        Status = UserStatus.Active,
                                                        Assessment = assessorAssessment.Assessment
                                                    });
                                            }
                                            else
                                            {
                                                existingAssessor.Assessment = assessorAssessment.Assessment;
                                            }

                                        }

                                        networkElementMilestone.AssessorUpdatedAt = DateTime.UtcNow;
                                        networkElementMilestone.AssessorUpdatedBy = request.UpdatedBy;
                                    }
                                });
                            }
                            else
                            {
                                networkElement.SoaMilestones?.Add(new SoaMilestone
                                {
                                    MilestoneId = Enum.Parse<Milestone>(milestone),
                                    AssessorUpdatedAt = DateTime.UtcNow,
                                    AssessorUpdatedBy = request.UpdatedBy,
                                    Assessors = request.AssessorAssessmentForElements.FirstOrDefault(e => e.ElementType == networkElement.ElementType)?.AssessorAssessments.Select(assessorAssessment => new SoaAssessorExistingNetwork
                                    {
                                        FirstName = assessorAssessment.AssessorFirstName,
                                        LastName = assessorAssessment.AssessorLastName,
                                        Email = assessorAssessment.AssessorEmail,
                                        Status = UserStatus.Active,
                                        Assessment = assessorAssessment.Assessment
                                    }).ToList() ?? new List<SoaAssessorExistingNetwork>()
                                });
                            }

                            _logger.LogInformation("Updated Assigned Assessor for existing HN ID: {HnId}, Element(s): {Element}", StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(string.Join(", ", networkElementsName)));
                        }
                    }
                }
                networkElements.ElementSoaStatus = NetworkDetailsStatus.InProgress;
                return networkElements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Assigned Assessor for existing HN ID: {HnId}, Element(s): {Element}", StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(string.Join(", ", networkElementsName)));
                throw;
            }

        }
    }
}
