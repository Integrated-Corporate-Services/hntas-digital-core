using FluentValidation;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/arms/v1/hn")]
    [ApiController]
    public class ArmsController : ControllerBase
    {
        private readonly IArmsKpiService _kpiService;
        private readonly ILogger<ArmsController> _logger;
        private readonly IValidator<KpiSubmissionRequest> _validator;

        public ArmsController(IArmsKpiService kpiService, ILogger<ArmsController> logger, IValidator<KpiSubmissionRequest> validator)
        {
            _kpiService = kpiService;
            _logger = logger;
            _validator = validator;
        }

        /// <summary>
        /// POST /api/arms/v1/hn/kpis
        /// Submits or updates high-level Key Performance Indicators (KPIs) for a specific Heat Network.
        /// </summary>
        [HttpPost("kpis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SubmitKpis([FromBody] KpiSubmissionRequest request)
        {
            _logger.LogInformation("ARMS KPI Request Received - Network: {NetworkId}, Period: {Period}",
                request.MetaData.NetworkId,
                request.MetaData.PeriodStart);

            try
            {
                var result = await _validator.ValidateAsync(request);

                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }
                    return ValidationProblem(ModelState);
                }

                var dataModel = new KpiSubmission
                {
                    MetaData = request.MetaData,
                    ConsumerConnectionAggregatedKpis = request.ConsumerConnectionAggregatedKpis,
                    Elements = request.Elements,
                    CreatedAt = DateTime.UtcNow,
                };

                var submissionId = await _kpiService.CreateOrUpdateSubmissionAsync(dataModel);

                return Ok(new
                {
                    submission_id = submissionId
                });
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Database connectivity error for Network: {NetworkId}, Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

                return Problem(
                    detail: "Database service temporarily unavailable.",
                    statusCode: 503,
                    title: "Service Unavailable"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal error processing submission for {NetworkId} during Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

                return Problem(
                     detail: "An unexpected error occurred while processing your request.",
                     statusCode: 500,
                     title: "Internal Server Error"
                 );
            }
        }
    }
}
