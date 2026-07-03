using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using System.Text.Json;

namespace HNTAS.Digital.Core.Tests.Services;

public class SubmissionCCServiceTests
{
    private readonly Mock<ICarbonCalculatorService> _ccService;
    private readonly Mock<IArmsKpiService> _kpiService;
    private readonly Mock<IHeatNetworkService> _heatNetworkService;
    private readonly Mock<ILogger<SubmissionCCService>> _logger;

    private readonly SubmissionCCService _sut;

    public SubmissionCCServiceTests()
    {
        _ccService = new Mock<ICarbonCalculatorService>();
        _kpiService = new Mock<IArmsKpiService>();
        _heatNetworkService = new Mock<IHeatNetworkService>();
        _logger = new Mock<ILogger<SubmissionCCService>>();

        _sut = new SubmissionCCService(
            _ccService.Object,
            _logger.Object,
            _kpiService.Object,
            _heatNetworkService.Object);
    }

    [Fact]
    public async Task Should_Return_When_No_EnergyCentre_Element_Exists()
    {
        var request = CreateValidRequest();
        request.Elements.Clear();

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(request, submission);

        _kpiService.Verify(
            x => x.GetConfigurationAsync(It.IsAny<string>()),
            Times.Never);


        _ccService.Verify(
            x => x.RunAsync(
                It.IsAny<CarbonCalculatorRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

    }

    [Fact]
    public async Task Should_Return_When_Config_Defaults_Are_Missing()
    {
        var request = CreateValidRequest();

        _kpiService
            .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
            .ReturnsAsync(new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Defaults = new Dictionary<string, BsonValue>()
                }
            });

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(request, submission);

        _ccService.Verify(
             x => x.RunAsync(
                 It.IsAny<CarbonCalculatorRequest>(),
                 It.IsAny<CancellationToken>()),
             Times.Never);
    }

    [Fact]
    public async Task Should_Return_When_PeriodStart_Is_Invalid()
    {
        var request = CreateValidRequest();
        request.MetaData.PeriodStart = "INVALID";

        SetupValidConfiguration();

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(request, submission);

        _ccService.Verify(
              x => x.RunAsync(
                  It.IsAny<CarbonCalculatorRequest>(),
                  It.IsAny<CancellationToken>()),
              Times.Never);
    }

    [Fact]
    public async Task Should_Calculate_Using_Current_Month_Only_For_January()
    {
        var request = CreateValidRequest();
        request.MetaData.PeriodStart = "2026-01";

        SetupValidConfiguration();
        SetupHeatNetwork();

        _ccService
            .Setup(x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse
            {
                TotalCarbonEmission = 100,
                Uuid = "test"
            });

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(
            request,
            submission);

        _kpiService.Verify(
            x => x.GetSubmissionsForYearAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Include_Historical_Submissions_In_Projection()
    {
        var request = CreateValidRequest();
        request.MetaData.PeriodStart = "2026-05";

        SetupValidConfiguration();
        SetupHeatNetwork();

        _kpiService
            .Setup(x => x.GetSubmissionsForYearAsync("HN2000002", 2026))
            .ReturnsAsync(new List<KpiSubmission>
            {
                CreateHistoricalSubmission("2026-01"),
                CreateHistoricalSubmission("2026-02")
            });

        CarbonCalculatorRequest captured = null;

        _ccService
            .Setup(x => x.RunAsync(
                It.IsAny<CarbonCalculatorRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<CarbonCalculatorRequest, CancellationToken>(
                (req, _) => captured = req)
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse());


        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(
            request,
            submission);

        Assert.NotNull(captured);
        Assert.NotNull(captured.Energy);
        Assert.NotEmpty(captured.Energy.ChpInputs);
    }

    [Fact]
    public async Task Should_Ignore_Historical_Submission_With_Null_Inputs()
    {
        var request = CreateValidRequest();

        SetupValidConfiguration();
        SetupHeatNetwork();

        _kpiService
            .Setup(x => x.GetSubmissionsForYearAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KpiSubmission>
            {
                new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "HN2000002",
                        PeriodStart = "2026-01"
                    },
                    CarbonCalculatorInputs = null
                }
            });

        _ccService
            .Setup(x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse());

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(
            request,
           submission);

        _ccService.Verify(
            x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Set_Boiler_Count_To_Zero_When_Blr_Section_Missing()
    {
        var request = CreateValidRequest();

        request.CarbonInputsV2.Remove("blr_totals");

        SetupValidConfiguration();
        SetupHeatNetwork();

        _kpiService
            .Setup(x => x.GetSubmissionsForYearAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KpiSubmission>
            {
                new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "HN2000002",
                        PeriodStart = "2026-01"
                    },
                    CarbonCalculatorInputs = null
                }
            });

        CarbonCalculatorRequest captured = null;

        _ccService
            .Setup(x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CarbonCalculatorRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse());

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(
            request,
            submission);

        Assert.NotNull(captured);
        Assert.Equal(0, captured.Energy.BoilerCount);
        Assert.Empty(captured.Energy.BoilerInputs);
    }

    [Fact]
    public async Task Should_Set_HeatPump_Count_To_Zero_When_Hpm_Section_Missing()
    {
        var request = CreateValidRequest();

        request.CarbonInputsV2.Remove("hpm_totals");

        SetupValidConfiguration();
        SetupHeatNetwork();

        _kpiService
            .Setup(x => x.GetSubmissionsForYearAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KpiSubmission>
            {
                new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "HN2000002",
                        PeriodStart = "2026-01"
                    },
                    CarbonCalculatorInputs = null
                }
            });

        CarbonCalculatorRequest captured = null;

        _ccService
            .Setup(x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CarbonCalculatorRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse());

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        await _sut.ProcessCarbonCalculationsAsync(
            request,
            submission);

        Assert.NotNull(captured);
        Assert.Equal(0, captured.Energy.HeatPumpCount);
        Assert.Empty(captured.Energy.HeatPumpInputs);
    }

    [Fact]
    public async Task Should_Update_Submission_With_Carbon_Calculation_Result()
    {
        var request = CreateValidRequest();

        SetupValidConfiguration();
        SetupHeatNetwork();

        _kpiService
            .Setup(x => x.GetSubmissionsForYearAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KpiSubmission>
            {
                new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "HN2000002",
                        PeriodStart = "2026-01"
                    },
                    CarbonCalculatorInputs = null
                }
            });

        var submission = new KpiSubmission { MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" } };

        _ccService
            .Setup(x => x.RunAsync(It.IsAny<CarbonCalculatorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HNTAS.Core.Api.Models.CarbonCalculatorResponse
            {
                TotalCarbonEmission = 999.5m,
                Uuid = "test-uuid"
            });

        await _sut.ProcessCarbonCalculationsAsync(
            request,
            submission);

        Assert.NotNull(submission.CarbonCalculatorResponse);
        Assert.Equal(999.5m, submission.CarbonCalculatorResponse.TotalCarbonEmission);
        Assert.Equal("test-uuid", submission.CarbonCalculatorResponse.Uuid);
    }

    private void SetupHeatNetwork()
    {
        _heatNetworkService
            .Setup(x => x.GetByHnIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new HeatNetwork
            {
                Name = "Test Network"
            });
    }

    private void SetupValidConfiguration()
    {
        var defaults = new Dictionary<string, BsonValue>
        {
            ["EC-DATA-20"] = "existing",
            ["EC-DATA-21"] = "both",
            ["EC-DATA-28"] = "M4 4HB",
            ["EC-DATA-32"] = "test@test.com",
            ["EC-DATA-35"] = "2026-01-01",
            ["EC-DATA-36"] = 1,
            ["EC-DATA-37"] = 2020,
            ["EC-DATA-38"] = 0,
            ["EC-DATA-50"] = 17,
            ["EC-DATA-51"] = "standard",
            ["EC-DATA-59"] = 0,
            ["EC-DATA-61"] = 0,
            ["EC-DATA-63"] = 1000,
            ["EC-DATA-64"] = 1200,
            ["EC-DATA-65"] = 11,
            ["EC-DATA-70"] = 0,
            ["EC-DATA-72"] = 0,
            ["EC-DATA-74"] = 1000,
            ["EC-DATA-83"] = 17,
            ["EC-DATA-88"] = 0,
            ["EC-DATA-90"] = 0,
            ["EC-DATA-92"] = 1000
        };

        _kpiService
            .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
            .ReturnsAsync(new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Defaults = defaults
                }
            });
    }

    private static KpiSubmissionRequestV2 CreateValidRequest()
    {
        return new KpiSubmissionRequestV2
        {
            MetaData = new KpiMetadata
            {
                NetworkId = "HN2000002",
                PeriodStart = "2026-05"
            },
            Elements = new List<NetworkElementRequest>
                {
                    new NetworkElementRequest
                    {
                        Type = HeatNetworkElementType.EnergyCentre.ToString(),
                        ElementId = "00001",
                        Kpis = new Dictionary<string, KpiValueRequest>
                        {
                            ["EC-KPI-01"] = new KpiValueRequest { Value = 10 }
                        }
                    }
            },
            CarbonInputsV2 = new Dictionary<string, Dictionary<string, CCKpiValueRequest>>
            {
                ["chp_totals"] = new()
                {
                    ["EC-DATA-47"] = new() { Value = JsonValue(100) },
                    ["EC-DATA-52"] = new() { Value = JsonValue("2026-05-29") },
                    ["EC-DATA-53"] = new() { Value = JsonValue(100) },
                    ["EC-DATA-55"] = new() { Value = JsonValue(100) },
                    ["EC-DATA-57"] = new() { Value = JsonValue(1000) }
                },

                ["hpm_totals"] = new()
                {
                    ["EC-DATA-66"] = new() { Value = JsonValue(1000) },
                    ["EC-DATA-68"] = new() { Value = JsonValue(1000) }
                },

                ["blr_totals"] = new()
                {
                    ["EC-DATA-84"] = new() { Value = JsonValue(1000) },
                    ["EC-DATA-86"] = new() { Value = JsonValue(1000) }
                }
            }
        };
    }


    private static JsonElement JsonValue(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }


    private static KpiSubmission CreateHistoricalSubmission(string periodStart)
    {
        return new KpiSubmission
        {
            MetaData = new KpiMetadata { NetworkId = "HN2000003", PeriodStart = "2026-01" },

            CarbonCalculatorInputs =
                new Dictionary<string, Dictionary<string, CCKpiValue>>
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-53"] = new()
                        {
                            Value = BsonValue.Create(100)
                        }
                    }
                }
        };
    }
}