using AutoMapper;
using FluentValidation;
using HNTAS.Core.Api.Common;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using HNTAS.Core.Api.Services;
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
        private readonly IValidator<KpiSubmissionRequestV2> _validator2;
        private readonly IHeatNetworkValidator _networkValidator;
        private readonly IKpiRuleValidator _ruleValidator;
        private readonly IMapper _mapper;
        private readonly ArmsSettings _armsSettings;
        private readonly ICarbonCalculatorService _CCService;

        public ArmsController(IArmsKpiService kpiService,
            ILogger<ArmsController> logger,
            IValidator<KpiSubmissionRequest> validator,
            IValidator<KpiSubmissionRequestV2> validator2,
            IMapper mapper,
            IOptions<ArmsSettings> armsSettings,
            IHeatNetworkValidator networkValidator,
            IKpiRuleValidator kpiRuleValidator,
            ICarbonCalculatorService CCService)
        {
            _kpiService = kpiService;
            _logger = logger;
            _validator = validator;
            _validator2 = validator2;
            _mapper = mapper;
            _armsSettings = armsSettings.Value;
            _networkValidator = networkValidator;
            _ruleValidator = kpiRuleValidator;
            _CCService = CCService;
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
                request.MetaData.NetworkId,
                request.MetaData.PeriodStart);

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
                        request.MetaData.NetworkId,
                        request.MetaData.PeriodStart);

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
                _logger.LogError(ex, "Database connectivity error for Network: {NetworkId}, Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

                return Problem(
                    detail: "Database service temporarily unavailable.",
                    statusCode: 503,
                    title: "Service Unavailable",
                    type: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal error processing submission for {NetworkId} during Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

                return Problem(
                     detail: "An unexpected error occurred while processing your request.",
                     statusCode: 500,
                     title: "Internal Server Error",
                     type: null
                 );
            }
        }

        /// <summary>
        /// POST /api/arms/v1/hn/kpis
        /// Submits or updates high-level Key Performance Indicators (KPIs) for a specific Heat Network.
        /// </summary>
        [HttpPost("/arms/v2/hn/kpis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(KpiSubmissionApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SubmitKpisV2([FromBody] KpiSubmissionRequestV2 request)
        {
            _logger.LogInformation("ARMS KPI Request Received - Network: {NetworkId}, Period: {Period}",
                 request.MetaData.NetworkId,
                 request.MetaData.PeriodStart);

            try
            {
                var schemaResult = await _validator2.ValidateAsync(request);

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
                        request.MetaData.NetworkId,
                        request.MetaData.PeriodStart);

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

                // Carbon calculations
                foreach (var element in request.Elements)
                {
                    if (element.Type == HeatNetworkElementType.EnergyCentre.ToString())
                    {
                        var inputs = element.CarbonInputsV2;
                        var dataModelElement = dataModel.Elements.FirstOrDefault(e => e.ElementId == element.ElementId);

                        // Extract sections safely
                        inputs.TryGetValue("background", out var backgroundSection);
                        inputs.TryGetValue("chp_totals", out var chpSection);
                        inputs.TryGetValue("hpm_totals", out var hpmSection);
                        inputs.TryGetValue("blr_totals", out var blrSection);

                        //mandatory
                        int ec47 = chpSection != null && chpSection.TryGetValue("EC-KPI-47", out var kpi47) && decimal.TryParse(kpi47?.Value?.ToString(), out var parsedDecimal47) ? Convert.ToInt32(parsedDecimal47) : 0;
                        int ec52 = chpSection != null && chpSection.TryGetValue("EC-KPI-52", out var kpi52) && decimal.TryParse(kpi52?.Value?.ToString(), out var parsedDecimal52) ? Convert.ToInt32(parsedDecimal52) : 0;
                        int ec54 = chpSection != null && chpSection.TryGetValue("EC-KPI-54", out var kpi54) && decimal.TryParse(kpi54?.Value?.ToString(), out var parsedDecimal54) ? Convert.ToInt32(parsedDecimal54) : 0;
                        int ec56 = chpSection != null && chpSection.TryGetValue("EC-KPI-56", out var kpi56) && decimal.TryParse(kpi56?.Value?.ToString(), out var parsedDecimal56) ? Convert.ToInt32(parsedDecimal56) : 0;

                        //optional
                        int ec65 = hpmSection != null && hpmSection.TryGetValue("EC-KPI-65", out var kpi65) && decimal.TryParse(kpi65?.Value?.ToString(), out var parsedDecimal65) ? Convert.ToInt32(parsedDecimal65) : 0;
                        int ec67 = hpmSection != null && hpmSection.TryGetValue("EC-KPI-67", out var kpi67) && decimal.TryParse(kpi67?.Value?.ToString(), out var parsedDecimal67) ? Convert.ToInt32(parsedDecimal67) : 0;
                        int ec83 = blrSection != null && blrSection.TryGetValue("EC-KPI-83", out var kpi83) && decimal.TryParse(kpi83?.Value?.ToString(), out var parsedDecimal83) ? Convert.ToInt32(parsedDecimal83) : 0;
                        int ec85 = blrSection != null && blrSection.TryGetValue("EC-KPI-85", out var kpi85) && decimal.TryParse(kpi85?.Value?.ToString(), out var parsedDecimal85) ? Convert.ToInt32(parsedDecimal85) : 0;

                        var requestModel = new CarbonCalculatorRequest
                        {
                            Background = new Background
                            {
                                // Mandatory field
                                DateWorkbookCompleted = backgroundSection != null && backgroundSection.TryGetValue("EC-KPI-19", out var kpi19)
                                    ? kpi19.Value.ToString()
                                    : null,
                                NetworkStatus = "existing",
                                NetworkServiceProvision = "both",
                                Name = "Arms Sample API Call",
                                NetworkID = "HN0001234",
                                NetworkName = "Sample Heat Network",
                                PostcodeOfThePrimaryEnergyCentre = "M4 4HB",
                                ContactEmail = "admin@sample.com",
                                CommissioningDate = "2026-09-14"
                            },
                            Energy = new Energy
                            {
                                YearCount = 1,
                                StartYear = 2026,
                                ChpCount = 1,
                                EnergyHeatNetworkPrimaryLosses = [0],
                                ChpInputs = new List<ChpInput>
                            {
                                new ChpInput
                                {
                                    ChpFuelTypeInput = 17,
                                    ChpInstallationDateInput = chpSection != null && chpSection.TryGetValue("EC-KPI-51", out var kpi51)
                                                            ? kpi51.Value.ToString()
                                                            : null,
                                    ChpOperationalModeInput = "export",
                                    ChpUsefulHeatValue = [ec52],
                                    ChpElectricityGeneratedValue = [ec54],
                                    ChpFuelUsedValue = [ec56],
                                    ChpHeatCoolingValue = [0],
                                    ChpSleevingPCentValue = [0],
                                    ChpMaxHeatOutput = 1000,
                                    ChpMaxElectricityOutput = 1200,
                                }
                            },
                                EppElectricityUsedForPumpingValue = [ec47],
                                BoilerCount = blrSection == null ? 0 : 1,
                                BoilerInputs = blrSection == null ? new List<BoilerInput>() : new List<BoilerInput>
                            {
                               new BoilerInput
                               {
                                   BlrTypeFuelUsedInput = 17,
                                   BlrUsefulHeatGeneratedValue = [ec83],
                                   BlrFuelUsedByValue = [ec85],
                                   BlrHeatUsedForCoolingProductionValue = [0],
                                   BlrSleevingPCentValue = [0],
                                   BlrMaxHeatOutput = 1000,
                               }
                            },
                                RecoveredCount = 0,
                                RecoveredInputs = new List<RecoveredInput>(),
                                HeatPumpCount = hpmSection == null ? 0 : 1,
                                HeatPumpInputs = hpmSection == null ? new List<HeatPumpInput>() : new List<HeatPumpInput>
                            {
                                new HeatPumpInput {
                                    HpmTypeFuelUsedInput = 11,
                                    HpmUsefulHeatGeneratedValue = [ec65],
                                    HpmEnergyUsedValue = [ec67],
                                    HpmUsefulCoolingGeneratedValue = [0],
                                    HpmSleevingPCentValue = [0],
                                    HpmMaxHeatOutput = 1000,
                                }
                            }
                            }
                        };

                        // Create carbon calculator inputs for backwards compatibility
                        var cc_result = await _CCService.RunAsync(requestModel);

                        dataModelElement.CarbonCalculatorResponse = new Data.Models.Arms.Submission.CarbonCalculatorResponse
                        {
                            TotalCarbonEmission = (decimal)(cc_result?.TotalCarbonEmission),
                            Uuid = cc_result?.Uuid
                        };
                    }
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
                _logger.LogError(ex, "Database connectivity error for Network: {NetworkId}, Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

                return Problem(
                    detail: "Database service temporarily unavailable.",
                    statusCode: 503,
                    title: "Service Unavailable",
                    type: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal error processing submission for {NetworkId} during Period: {Period}. TraceId: {TraceId}", request.MetaData.NetworkId, request.MetaData.PeriodStart, HttpContext.TraceIdentifier);

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
            _logger.LogInformation("ARMS KPI Config Request Received for Network: {NetworkId}", networkId);

            if (string.IsNullOrEmpty(networkId) || !Regex.IsMatch(networkId, @"^HN[0-9]{7}$"))
            {
                _logger.LogWarning("Invalid NetworkId format received: {NetworkId}", networkId);
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
                    _logger.LogWarning("KPI Config search returned no results for Network: {NetworkId}", networkId);
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
                _logger.LogError(ex, "Error retrieving KPI Config for Network: {NetworkId}", networkId);

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
        /// Gets the KPI configuration for a specific Heat Network. This includes the list of KPIs that should be reported, along with any metadata or validation rules associated with those KPIs.
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        //[HttpGet("/arms/v2/hn/{networkId}/kpi-config")]
        //[ProducesResponseType(typeof(KpiConfigResponse), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        //public async Task<ActionResult<KpiConfigResponse>> GetKpiConfigV2(string networkId)
        //{
        //    _logger.LogInformation("ARMS KPI Config Request Received for Network: {NetworkId}", networkId);

        //    if (string.IsNullOrEmpty(networkId) || !Regex.IsMatch(networkId, @"^HN[0-9]{7}$"))
        //    {
        //        _logger.LogWarning("Invalid NetworkId format received: {NetworkId}", networkId);
        //        return BadRequest(new ProblemDetails
        //        {
        //            Status = StatusCodes.Status400BadRequest,
        //            Title = "Invalid NetworkId",
        //            Detail = "The NetworkId must follow the format: HN followed by 7 digits (e.g., HN1234567).",
        //            Type = null
        //        });
        //    }

        //    try
        //    {
        //        var config = await _kpiService.GetConfigurationAsync(networkId);

        //        if (config == null)
        //        {
        //            _logger.LogWarning("KPI Config search returned no results for Network: {NetworkId}", networkId);
        //            return NotFound(new ProblemDetails
        //            {
        //                Status = StatusCodes.Status404NotFound,
        //                Title = "Configuration Not Found",
        //                Detail = $"No KPI configuration could be found for the network ID: {networkId}.",
        //                Type = null
        //            });
        //        }

        //        var response = _mapper.Map<KpiConfigResponse>(config);

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving KPI Config for Network: {NetworkId}", networkId);

        //        return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        //        {
        //            Status = StatusCodes.Status500InternalServerError,
        //            Title = "Error retrieving configuration",
        //            Detail = "An unexpected error occurred while fetching the KPI configuration.",
        //            Type = null
        //        });
        //    }
        //}

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
            _logger.LogInformation("Received request to Save/Update KPI Config for NetworkId: {NetworkId}", request?.NetworkId);

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
                    Detail = ex.Message,
                    Type = null
                });
            }
        }


        /// <summary>
        /// Creates or updates the KPI configuration for a specific network.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        //[HttpPost("/arms/v2/hn/kpi-config")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> SaveConfigV2([FromBody] KpiConfigRequestV2 request)
        //{
        //    _logger.LogInformation("Received request to Save/Update KPI Config for NetworkId: {NetworkId}", request?.NetworkId);

        //    if (request == null || string.IsNullOrEmpty(request.NetworkId))
        //    {
        //        _logger.LogWarning("SaveConfig failed: Request body or NetworkId is missing.");
        //        return StatusCode(StatusCodes.Status400BadRequest, new ProblemDetails
        //        {
        //            Status = StatusCodes.Status500InternalServerError,
        //            Title = "An error occurred while processing your request.",
        //            Detail = "Invalid configuration data.",
        //            Type = null
        //        });
        //    }

        //    try
        //    {
        //        var configModel = _mapper.Map<KpiConfiguration>(request);

        //        await _kpiService.CreateOrUpdateConfigurationAsync(configModel);

        //        _logger.LogInformation("Successfully saved KPI Configuration for NetworkId: {NetworkId}", request.NetworkId);

        //        return Ok(new { message = "Configuration saved successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while saving KPI Configuration for NetworkId: {NetworkId}", request.NetworkId);

        //        return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        //        {
        //            Status = StatusCodes.Status500InternalServerError,
        //            Title = "An error occurred while processing your request.",
        //            Detail = ex.Message,
        //            Type = null
        //        });
        //    }
        //}


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
