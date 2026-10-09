using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms.Dashboard;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class KpiSubmissionAuditService : IKpiSubmissionAuditService
    {
        private readonly IMongoCollection<KpiSubmissionAudit> _auditCollection;

        public KpiSubmissionAuditService(IMongoDatabase mongoDatabase, IOptions<AWSDocDbSettings> dbSettings)
        {
            _auditCollection = mongoDatabase.GetCollection<KpiSubmissionAudit>(dbSettings.Value.AuditKpiSubmissionCollectionName);
        }


        public async Task<IEnumerable<KpiHistoryResponse>> GetHistoryBySubmissionIdAsync(string submissionId)
        {
            // 1. Fetch ALL audit logs for this submission
            var auditLogs = await _auditCollection
                .Find(x => x.SubmissionId == submissionId)
                .ToListAsync();

            if (auditLogs == null || !auditLogs.Any())
                return Enumerable.Empty<KpiHistoryResponse>();

            // 2. Flatten and Group
            return auditLogs.SelectMany(doc =>
                doc.Changes
                    .GroupBy(c => new { c.ElementId, c.KpiId, c.Aggregated })
                    .Select(g =>
                    {
                        var valueChange = g.FirstOrDefault(x => string.Equals(x.Property, "value", StringComparison.OrdinalIgnoreCase));
                        var statusChange = g.FirstOrDefault(x => string.Equals(x.Property, "assessmentStatus", StringComparison.OrdinalIgnoreCase));
                        var imputationChange = g.FirstOrDefault(x => string.Equals(x.Property, "isKpiImputed", StringComparison.OrdinalIgnoreCase) ||
                                                                   string.Equals(x.Property, "isImputed", StringComparison.OrdinalIgnoreCase));

                        // Identify if this row belongs to a Carbon Calculator Input metric
                        bool isCarbonInput = g.Key.ElementId == null && !g.Key.Aggregated && g.Key.KpiId != null;

                        // Fallback text assignments based on the metric type
                        string fallbackStatus = isCarbonInput ? "N/A" : "No Change";

                        return new KpiHistoryResponse
                        {
                            Timestamp = doc.Timestamp,
                            SourceSystem = doc.SourceSystem,
                            KpiId = g.Key.KpiId,
                            ElementId = g.Key.ElementId ?? (g.Key.Aggregated ? "Aggregated" : null),
                            IsAggregated = g.Key.Aggregated,

                            OldValue = valueChange?.Old?.ToString(),
                            NewValue = valueChange?.New?.ToString(),

                            // FIX: Uses "N/A" for Carbon Inputs, and "No Change" for standard KPIs
                            OldStatus = statusChange == null ? fallbackStatus : TranslateStatus(statusChange.Old),
                            NewStatus = statusChange == null ? fallbackStatus : TranslateStatus(statusChange.New),

                            IsImputed = (bool?)imputationChange?.New ?? false
                        };
                    })
            )
            .OrderByDescending(x => x.Timestamp)
            .ToList();
        }

        private string TranslateStatus(object status) => status?.ToString() switch
        {
            "1" => "Pass",
            "2" => "Fail",
            "3" => "Outside Limit",
            "0" => "Undefined",
            null => "N/A",
            _ => "N/A"
        };

        public async Task TrackChangesAsync(KpiSubmission existing, KpiSubmission incoming)
        {
            var deltas = new List<KpiDeltaAudit>();

            // 1. Process Aggregated KPIs (Using KpiValueAggregated)
            if (incoming.ConsumerConnectionAggregatedKpis != null)
            {
                foreach (var (kpiId, incomingKpi) in incoming.ConsumerConnectionAggregatedKpis)
                {
                    existing.ConsumerConnectionAggregatedKpis.TryGetValue(kpiId, out var existingKpi);
                    deltas.AddRange(CalculateAggregatedDeltas(kpiId, existingKpi, incomingKpi));
                }
            }

            // 2. Process Element KPIs (Using KpiValue)
            if (incoming.Elements != null)
            {
                foreach (var incomingEl in incoming.Elements)
                {
                    var existingEl = existing.Elements?.FirstOrDefault(e => e.ElementId == incomingEl.ElementId);
                    foreach (var (kpiId, incomingKpi) in incomingEl.Kpis)
                    {
                        var existingKpi = existingEl?.Kpis?.GetValueOrDefault(kpiId);
                        deltas.AddRange(CalculateStandardDeltas(incomingEl.ElementId, kpiId, existingKpi, incomingKpi));
                    }
                }
            }

            // 3. Process Carbon Calculator Metric Inputs
            if (incoming.CarbonCalculatorInputs != null)
            {
                var inputDeltas = CalculateCarbonInputDeltas(existing.CarbonCalculatorInputs, incoming.CarbonCalculatorInputs);
                if (inputDeltas != null && inputDeltas.Any())
                {
                    deltas.AddRange(inputDeltas);
                }
            }

            if (deltas.Any())
            {
                var auditDoc = new KpiSubmissionAudit
                {
                    NetworkId = incoming.MetaData.NetworkId,
                    SubmissionId = existing.Id!,
                    Timestamp = DateTime.UtcNow,
                    SourceSystem = incoming.MetaData.SourceSystem,
                    PeriodStart = incoming.MetaData.PeriodStart,
                    Changes = deltas
                };
                await _auditCollection.InsertOneAsync(auditDoc);
            }
        }


        // 2. Comparison for Root-Level Dictionary Inputs (Updated target)
        private List<KpiDeltaAudit> CalculateCarbonInputDeltas(
    Dictionary<string, Dictionary<string, CCKpiValue>>? oldSections,
    Dictionary<string, Dictionary<string, CCKpiValue>> newSections)
        {
            var list = new List<KpiDeltaAudit>();

            foreach (var (sectionName, newMetrics) in newSections)
            {
                // Try to get the matching section from the old submission data
                Dictionary<string, CCKpiValue>? oldMetrics = null;
                oldSections?.TryGetValue(sectionName, out oldMetrics);

                foreach (var (metricCode, @new) in newMetrics)
                {
                    // Try to find the matching previous KPI data layout
                    CCKpiValue? old = null;
                    oldMetrics?.TryGetValue(metricCode, out old);

                    // 1. Convert to uniform string representations for an accurate comparison check
                    string oldStringValue = old?.Value?.ToString() ?? string.Empty;
                    string newStringValue = @new.Value?.ToString() ?? string.Empty;

                    if (oldStringValue != newStringValue)
                    {
                        // Extract native C# primitive values (int, double, or string) safely
                        object parsedOld = GetNativeValue(old?.Value);
                        object parsedNew = GetNativeValue(@new.Value);

                        // If a brand new item was added, ensure the old baseline displays as 0 instead of an empty string
                        if (string.IsNullOrEmpty(oldStringValue))
                        {
                            parsedOld = 0;
                        }

                        // Add Value Change Audit record
                        list.Add(new KpiDeltaAudit
                        {
                            ElementId = null,
                            Aggregated = false,
                            KpiId = metricCode,
                            Property = "Value",
                            Old = parsedOld, // Saves cleanly to MongoDB without _t and _v structures
                            New = parsedNew
                        });
                    }

                    // 2. Audit 'IsImputed' changes
                    if (old?.IsImputed != @new.IsImputed)
                    {
                        list.Add(new KpiDeltaAudit
                        {
                            ElementId = null,
                            Aggregated = false,
                            KpiId = metricCode,
                            Property = "IsImputed",
                            Old = old?.IsImputed,
                            New = @new.IsImputed
                        });
                    }

                    // 3. Audit 'ImputationDetails' changes
                    if (old?.ImputationDetails != @new.ImputationDetails)
                    {
                        list.Add(new KpiDeltaAudit
                        {
                            ElementId = null,
                            Aggregated = false,
                            KpiId = metricCode,
                            Property = "ImputationDetails",
                            Old = old?.ImputationDetails,
                            New = @new.ImputationDetails
                        });
                    }
                }
            }

            return list;
        }

        private object GetNativeValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return 0; // Default fallback for missing values
            }

            // Unbox MongoDB specific BSON types to raw C# primitives
            if (rawValue is MongoDB.Bson.BsonInt32 bInt) return bInt.Value;
            if (rawValue is MongoDB.Bson.BsonDouble bDbl) return bDbl.Value;
            if (rawValue is MongoDB.Bson.BsonInt64 bLng) return bLng.Value;
            if (rawValue is MongoDB.Bson.BsonString bStr) return bStr.Value;

            // If it's already a native C# numeric primitive, return it directly
            if (rawValue is int or double or decimal or long)
            {
                return rawValue;
            }

            // If it's a string representation, check if it's a numeric value or text/date string
            string stringValue = rawValue.ToString() ?? string.Empty;
            return double.TryParse(stringValue, out var parsedDouble) ? parsedDouble : stringValue;
        }

        private List<KpiDeltaAudit> CalculateAggregatedDeltas(string kpiId, KpiValueAggregated old, KpiValueAggregated @new)
        {
            var list = new List<KpiDeltaAudit>();
            if (old?.Value != @new.Value)
                list.Add(CreateDelta(null, true, kpiId, "Value", old?.Value, @new.Value));

            if (old?.AssessmentStatus != @new.AssessmentStatus)
                list.Add(CreateDelta(null, true, kpiId, "AssessmentStatus", old?.AssessmentStatus, @new.AssessmentStatus));

            return list;
        }

        // Comparison for Standard (Includes Imputation fields)
        private List<KpiDeltaAudit> CalculateStandardDeltas(string elId, string kpiId, KpiValue old, KpiValue @new)
        {
            var list = new List<KpiDeltaAudit>();

            if (old?.Value != @new.Value)
                list.Add(CreateDelta(elId, false, kpiId, "Value", old?.Value, @new.Value));

            if (old?.AssessmentStatus != @new.AssessmentStatus)
                list.Add(CreateDelta(elId, false, kpiId, "AssessmentStatus", old?.AssessmentStatus, @new.AssessmentStatus));

            if (old?.IsKpiImputed != @new.IsKpiImputed)
                list.Add(CreateDelta(elId, false, kpiId, "IsKpiImputed", old?.IsKpiImputed, @new.IsKpiImputed));

            if (old?.KpiImputationDetails != @new.KpiImputationDetails)
                list.Add(CreateDelta(elId, false, kpiId, "KpiImputationDetails", old?.KpiImputationDetails, @new.KpiImputationDetails));

            return list;
        }

        private KpiDeltaAudit CreateDelta(string elId, bool agg, string kpiId, string prop, object oldVal, object newVal)
        {
            return new KpiDeltaAudit { ElementId = elId, Aggregated = agg, KpiId = kpiId, Property = prop, Old = oldVal, New = newVal };
        }
    }
}
