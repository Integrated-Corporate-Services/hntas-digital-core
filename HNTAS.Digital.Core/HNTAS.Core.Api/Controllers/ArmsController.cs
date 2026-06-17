using AutoMapper;
using FluentValidation;
using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Validators.Arms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace HNTAS.Core.Api.Controllers
{
    [Route("arms/v1/hn")]
    [ApiController]
    public class ArmsController : ControllerBase
    {
        private readonly IArmsKpiService _kpiService;
        private readonly ILogger<ArmsController> _logger;
        private readonly IValidator<KpiSubmissionRequest> _validator;
        private readonly IHeatNetworkValidator _networkValidator;
        private readonly IKpiRuleValidator _ruleValidator;
        private readonly IMapper _mapper;
        private readonly ArmsSettings _armsSettings;

        public ArmsController(IArmsKpiService kpiService,
            ILogger<ArmsController> logger,
            IValidator<KpiSubmissionRequest> validator,
            IMapper mapper,
            IOptions<ArmsSettings> armsSettings,
            IHeatNetworkValidator networkValidator,
            IKpiRuleValidator kpiRuleValidator)
        {
            _kpiService = kpiService;
            _logger = logger;
            _validator = validator;
            _mapper = mapper;
            _armsSettings = armsSettings.Value;
            _networkValidator = networkValidator;
            _ruleValidator = kpiRuleValidator;
        }

        /// <summary>
        /// POST /api/arms/v1/hn/kpis
        /// Submits or updates high-level Key Performance Indicators (KPIs) for a specific Heat Network.
        /// </summary>
        [HttpPost("kpis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(KpiSubmissionApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SubmitKpis([FromBody] KpiSubmissionRequest request)
        {
            _logger.LogInformation("ARMS KPI Request Received - Network: {NetworkId}, Period: {Period}",
                request.MetaData.NetworkId.ToSafeLog(),
                request.MetaData.PeriodStart.ToSafeLog());

            try
            {
                var schemaResult = await _validator.ValidateAsync(request);

                if (!schemaResult.IsValid)
                {
                    var apiErrors = schemaResult.Errors.Select(e =>
                    {
                        // Access the custom state we tucked away in the validator
                        var state = e.CustomState as dynamic;

                        return new KpiSubmissionApiError
                        {
                            Code = e.ErrorCode,
                            Message = e.ErrorMessage,
                            // Pull directly from state if it exists, otherwise fallback to null
                            ElementId = state?.elementId?.ToString(),
                            Kpis = state?.kpis is List<string> list ? list : null
                        };
                    }).ToList();

                    return BadRequest(new KpiSubmissionApiErrorResponse
                    {
                        Title = "Validation Failed",
                        Status = 400,
                        Detail = "The request format is invalid.",
                        Errors = apiErrors
                    });
                }

                var dataModel = _mapper.Map<KpiSubmission>(request);

                if (_armsSettings.EnableExtendedValidation)
                {
                    _logger.LogInformation("Extended validation is enabled. Performing additional checks for Network: {NetworkId}, Period: {Period}",
                        request.MetaData.NetworkId.ToSafeLog(),
                        request.MetaData.PeriodStart.ToSafeLog());

                    // Registry Validation (HeatNetwork Collection)
                    var networkResult = await _networkValidator.ValidateAsync(
                        request.MetaData.NetworkId,
                        request.Elements
                    );
                    if (!networkResult.IsValid)
                        return StatusCode(networkResult.StatusCode, CreateProblem(networkResult));

                    // Configuration Validation (KPI_Config Collection)
                    var ruleResult = await _ruleValidator.ValidateAsync(dataModel);
                    if (!ruleResult.IsValid)
                        return StatusCode(ruleResult.StatusCode, CreateProblem(ruleResult));
                }

                var submissionId = await _kpiService.CreateOrUpdateSubmissionAsync(dataModel);

                // 1. Extract flattened warnings, filtering OUT "pass" statuses
                var warnings = dataModel.Elements
                    .SelectMany(e => e.Kpis
                        .Where(k => k.Value.AssessmentStatus != KPIAssessmentStatus.Pass) // Exclude Pass
                        .Select(k => new
                        {
                            code = "KPI_EVALUATION",
                            elementId = e.ElementId,
                            kpi = k.Key,
                            status = FormatStatus(k.Value.AssessmentStatus)
                        }))
                    .ToList();

                // 2. Add flattened warnings from aggregated KPIs (excluding Pass)
                if (dataModel.ConsumerConnectionAggregatedKpis != null)
                {
                    var aggWarnings = dataModel.ConsumerConnectionAggregatedKpis
                        .Where(k => k.Value.AssessmentStatus != KPIAssessmentStatus.Pass) // Exclude Pass
                        .Select(k => new
                        {
                            code = "KPI_EVALUATION",
                            elementId = "Aggregated",
                            kpi = k.Key,
                            status = FormatStatus(k.Value.AssessmentStatus)
                        });

                    warnings.AddRange(aggWarnings);
                }

                // 3. Return the response
                return Ok(new
                {
                    submission_id = submissionId,
                    message = warnings.Any()
                        ? "Submission accepted, but some items require review."
                        : "Submission accepted successfully.",
                    warnings = warnings
                });
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Database connectivity error for Network: {NetworkId}, Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId.ToSafeLog(), request.MetaData.PeriodStart.ToSafeLog(), HttpContext.TraceIdentifier);

                return Problem(
                    detail: "Database service temporarily unavailable.",
                    statusCode: 503,
                    title: "Service Unavailable",
                    type: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal error processing submission for {NetworkId} during Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId.ToSafeLog(), request.MetaData.PeriodStart.ToSafeLog(), HttpContext.TraceIdentifier);

                return Problem(
                     detail: "An unexpected error occurred while processing your request.",
                     statusCode: 500,
                     title: "Internal Server Error",
                     type: null
                 );
            }
        }

        private string? ExtractId(string propertyName)
        {
            // Simple logic to pull numbers/IDs out of brackets like [0] or [CC-KPI-03]
            var match = System.Text.RegularExpressions.Regex.Match(propertyName, @"\[(.*?)\]");
            return match.Success ? match.Groups[1].Value : null;
        }

        string FormatStatus(KPIAssessmentStatus status) => status switch
        {
            KPIAssessmentStatus.OutsideLimit => "outside limit",
            _ => status.ToString().ToLower()
        };


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
            _logger.LogInformation("ARMS KPI Config Request Received for Network: {NetworkId}", networkId.ToSafeLog());

            if (string.IsNullOrEmpty(networkId) || !Regex.IsMatch(networkId, @"^HN[0-9]{7}$"))
            {
                _logger.LogWarning("Invalid NetworkId format received: {NetworkId}", networkId.ToSafeLog());
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid NetworkId",
                    Detail = "The NetworkId must follow the format: HN followed by 7 digits (e.g., HN1234567).",
                    Type = null
                });
            }

            try
            {
                var config = await _kpiService.GetConfigurationAsync(networkId);

                if (config == null)
                {
                    _logger.LogWarning("KPI Config search returned no results for Network: {NetworkId}", networkId.ToSafeLog());
                    return NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Configuration Not Found",
                        Detail = $"No KPI configuration could be found for the network ID: {networkId}.",
                        Type = null
                    });
                }

                var response = _mapper.Map<KpiConfigResponse>(config);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KPI Config for Network: {NetworkId}", networkId.ToSafeLog());

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Error retrieving configuration",
                    Detail = "An unexpected error occurred while fetching the KPI configuration.",
                    Type = null
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveConfig([FromBody] KpiConfigRequest request)
        {
            _logger.LogInformation("Received request to Save/Update KPI Config for NetworkId: {NetworkId}", request?.NetworkId.ToSafeLog());

            if (request == null || string.IsNullOrEmpty(request.NetworkId))
            {
                _logger.LogWarning("SaveConfig failed: Request body or NetworkId is missing.");
                return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Detail = "Invalid configuration data.",
                    Type = null
                });
            }

            try
            {
                var configModel = _mapper.Map<KpiConfiguration>(request);

                await _kpiService.CreateOrUpdateConfigurationAsync(configModel);

                _logger.LogInformation("Successfully saved KPI Configuration for NetworkId: {NetworkId}", request.NetworkId.ToSafeLog());

                return Ok(new { message = "Configuration saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving KPI Configuration for NetworkId: {NetworkId}", request.NetworkId.ToSafeLog());

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request.",
                    Detail = ex.Message,
                    Type = null
                });
            }
        }


        // Helper to keep the "Type" link out of our GOV.UK style response
        private object CreateProblem(ValidationGateResult result)
        {
            // We return a plain object or a custom class to avoid the 
            // default Dictionary behavior of ValidationProblemDetails
            return new KpiSubmissionApiErrorResponse
            {
                Title = "Validation Failed",
                Status = result.StatusCode,
                Detail = !string.IsNullOrWhiteSpace(result.Detail)
                         ? result.Detail
                         : result.Message ?? "One or more validation errors occurred.",
                Errors = result.Errors ?? new List<KpiSubmissionApiError>()
            };
        }
    }
}
