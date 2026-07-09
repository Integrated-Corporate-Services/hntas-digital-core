using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms.PowerBi;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArmsReportController : ControllerBase
    {
        private readonly IArmsPowerBiService _armsPowerBiService;
        private readonly ILogger<ArmsReportController> _logger;

        public ArmsReportController(IArmsPowerBiService armsPowerBiService, ILogger<ArmsReportController> logger)
        {
            _armsPowerBiService = armsPowerBiService;
            _logger = logger;
        }

        [HttpGet("powerbi-data")]
        [ProducesResponseType(typeof(IEnumerable<ArmsPowerBiReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPowerBiData()
        {
            try
            {
                _logger.LogInformation("Starting retrieval of ARMS Power BI report data.");

                var data = await _armsPowerBiService.GetPowerBiDataAsync();

                if (data == null || !data.Any())
                {
                    _logger.LogWarning("No data returned from ArmsPowerBiService.");
                    return Ok(new List<ArmsPowerBiReportResponse>());
                }

                // 1. Extract regular element KPIs
                var elementKpis = data.SelectMany(submission => submission.KpiSubmission.Elements
                    .SelectMany(element => element.Kpis.Select(kpi => new ArmsPowerBiReportResponse
                    {
                        HnId = submission.KpiSubmission.MetaData.NetworkId,
                        OrgId = submission.OrgId,
                        PeriodStart = submission.KpiSubmission.MetaData.PeriodStart,
                        ElementId = element.ElementId,
                        ElementType = element.Type.GetDescription(),
                        KpiId = kpi.Key,
                        Value = kpi.Value?.Value ?? 0,
                        AssessmentStatus = kpi.Value?.AssessmentStatus.GetDescription() ?? string.Empty
                    })));

                // 2. Extract consumer connection aggregated KPIs
                var aggregatedKpis = data.SelectMany(submission => (submission.KpiSubmission.ConsumerConnectionAggregatedKpis ?? Enumerable.Empty<KeyValuePair<string, KpiValueAggregated>>())
                     .Select(kpi => new ArmsPowerBiReportResponse
                     {
                         HnId = submission.KpiSubmission.MetaData.NetworkId,
                         OrgId = submission.OrgId,
                         PeriodStart = submission.KpiSubmission.MetaData.PeriodStart,
                         ElementId = null,
                         ElementType = "Aggregated",
                         KpiId = kpi.Key,
                         Value = kpi.Value?.Value ?? 0,
                         AssessmentStatus = kpi.Value?.AssessmentStatus.GetDescription() ?? string.Empty
                     }));

                // 3. Combine both collections cleanly into a single list
                var response = elementKpis.Concat(aggregatedKpis).ToList();

                _logger.LogInformation("Successfully processed {Count} rows for Power BI extraction.", response.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the ARMS Power BI requirement.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving data for Power BI.");
            }
        }
    }
}
