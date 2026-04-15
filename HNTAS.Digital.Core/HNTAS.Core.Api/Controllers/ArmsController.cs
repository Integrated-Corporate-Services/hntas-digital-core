using AutoMapper;
using FluentValidation;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HNTAS.Core.Api.Controllers
{
    [Route("v1/hn")]
    [ApiController]
    public class ArmsController : ControllerBase
    {
        private readonly IArmsKpiService _kpiService;
        private readonly ILogger<ArmsController> _logger;
        private readonly IValidator<KpiSubmissionRequest> _validator;
        private readonly IMapper _mapper;

        public ArmsController(IArmsKpiService kpiService, ILogger<ArmsController> logger, IValidator<KpiSubmissionRequest> validator, IMapper mapper)
        {
            _kpiService = kpiService;
            _logger = logger;
            _validator = validator;
            _mapper = mapper;
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

                var dataModel = _mapper.Map<KpiSubmission>(request);

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


        /// <summary>
        /// Gets the KPI configuration for a specific Heat Network. This includes the list of KPIs that should be reported, along with any metadata or validation rules associated with those KPIs.
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        [HttpGet("{networkId}/kpi-config")]
        [ProducesResponseType(typeof(KpiConfigResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<KpiConfigResponse>> GetKpiConfig(string networkId)
        {
            _logger.LogInformation("ARMS KPI Config Request Received for Network: {NetworkId}", networkId);

            if (string.IsNullOrEmpty(networkId) || !Regex.IsMatch(networkId, @"^HN[0-9]{7}$"))
            {
                _logger.LogWarning("Invalid NetworkId format received: {NetworkId}", networkId);
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid NetworkId",
                    Detail = "The NetworkId must follow the format: HN followed by 7 digits (e.g., HN1234567)."
                });
            }

            try
            {
                var config = await _kpiService.GetConfigurationAsync(networkId);

                if (config == null)
                {
                    _logger.LogWarning("KPI Config search returned no results for Network: {NetworkId}", networkId);
                    return NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Configuration Not Found",
                        Detail = $"No KPI configuration could be found for the network ID: {networkId}."
                    });
                }

                var response = _mapper.Map<KpiConfigResponse>(config);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KPI Config for Network: {NetworkId}", networkId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Error retrieving configuration",
                    Detail = "An unexpected error occurred while fetching the KPI configuration."
                });
            }
        }

        /// <summary>
        /// Creates or updates the KPI configuration for a specific network.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("kpi-config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveConfig([FromBody] KpiConfigRequest request)
        {
            _logger.LogInformation("Received request to Save/Update KPI Config for NetworkId: {NetworkId}", request?.NetworkId);

            if (request == null || string.IsNullOrEmpty(request.NetworkId))
            {
                _logger.LogWarning("SaveConfig failed: Request body or NetworkId is missing.");
                return BadRequest("Invalid configuration data.");
            }

            try
            {
                var configModel = _mapper.Map<KpiConfiguration>(request);

                await _kpiService.CreateOrUpdateConfigurationAsync(configModel);

                _logger.LogInformation("Successfully saved KPI Configuration for NetworkId: {NetworkId}", request.NetworkId);

                return Ok(new { message = "Configuration saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving KPI Configuration for NetworkId: {NetworkId}", request.NetworkId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Detail = ex.Message
                });
            }
        }
    }
}
