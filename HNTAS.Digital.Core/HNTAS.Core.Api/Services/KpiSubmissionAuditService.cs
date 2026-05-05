using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms.Dashboard;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class KpiSubmissionAuditService : IKpiSubmissionAuditService
    {
        private readonly IMongoCollection<KpiSubmissionAudit> _auditCollection;

        public KpiSubmissionAuditService(IMongoDatabase mongoDatabase)
        {
            _auditCollection = mongoDatabase.GetCollection<KpiSubmissionAudit>("Audit_KpiSubmission");
        }


        public async Task<IEnumerable<KpiHistoryResponse>> GetHistoryBySubmissionIdAsync(string submissionId)
        {
            // 1. Fetch ALL audit logs for this submission, not just the first one
            var auditLogs = await _auditCollection
                .Find(x => x.SubmissionId == submissionId)
                .ToListAsync();

            if (auditLogs == null || !auditLogs.Any())
                return Enumerable.Empty<KpiHistoryResponse>();

            // 2. Use SelectMany to flatten the documents and their changes
            return auditLogs.SelectMany(doc =>
                doc.Changes
                    // 3. Group by Element, KPI, and Timestamp to merge "Value" and "Status" rows
                    .GroupBy(c => new { c.ElementId, c.KpiId, c.Aggregated })
                    .Select(g => new KpiHistoryResponse
                    {
                        Timestamp = doc.Timestamp, // Use the timestamp from the parent document
                        SourceSystem = doc.SourceSystem,
                        KpiId = g.Key.KpiId,
                        ElementId = g.Key.ElementId ?? "Aggregated",
                        IsAggregated = g.Key.Aggregated,

                        // Combine the "Value" property change
                        OldValue = g.FirstOrDefault(x => x.Property == "Value")?.Old?.ToString(),
                        NewValue = g.FirstOrDefault(x => x.Property == "Value")?.New?.ToString(),

                        // Combine the "AssessmentStatus" property change
                        OldStatus = TranslateStatus(g.FirstOrDefault(x => x.Property == "AssessmentStatus")?.Old),
                        NewStatus = TranslateStatus(g.FirstOrDefault(x => x.Property == "AssessmentStatus")?.New),

                        IsImputed = (bool?)g.FirstOrDefault(x => x.Property == "IsKpiImputed")?.New ?? false
                    })
            )
            .OrderByDescending(x => x.Timestamp) // Show newest changes at the top
            .ToList();
        }

        private string TranslateStatus(object status) => status?.ToString() switch
        {
            "1" => "Pass",
            "2" => "Fail",
            "3" => "Outside Limit",
            "0" => "Undefined",
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
