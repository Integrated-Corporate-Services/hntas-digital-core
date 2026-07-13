using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using HNTAS.Core.Api.Validators.Arms;
using System.Text.Json;

namespace HNTAS.Digital.Core.Tests.Validators;

public class KpiCarbonInputsValidatorTests
{
    private readonly KpiCarbonInputsValidator _validator;

    public KpiCarbonInputsValidatorTests()
    {
        _validator = new KpiCarbonInputsValidator();
    }


    private static JsonElement J(object value) =>
        JsonSerializer.SerializeToElement(value);

    private static KpiSubmissionRequestV2 BaseRequest(bool withEnergyCentre = true)
    {
        return new KpiSubmissionRequestV2
        {
            MetaData = new KpiMetadata
            {
                NetworkId = "HN2000002",
                PeriodStart = "2026-01"
            },
            Elements = withEnergyCentre
                ? new List<NetworkElementRequest>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.EnergyCentre.ToString(),
                        ElementId = "EC1"
                    }
                }
                : new List<NetworkElementRequest>(),

            CarbonInputsV2 = new()
            {
                ["chp_totals"] = new()
                {
                    ["EC-DATA-52"] = new() { Value = J("2026-05-29") },
                    ["EC-DATA-53"] = new() { Value = J(100) },
                    ["EC-DATA-55"] = new() { Value = J(100) },
                    ["EC-DATA-57"] = new() { Value = J(1000) }
                }
            }
        };
    }

    [Fact]
    public async Task Should_Pass_When_No_EnergyCentre_Present()
    {
        var request = BaseRequest(withEnergyCentre: false);

        request.CarbonInputsV2 = null;

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Should_Fail_When_CarbonInputs_Missing()
    {
        var request = BaseRequest();
        request.CarbonInputsV2 = null;

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_CARBON_INPUTS");
    }

    [Fact]
    public async Task Should_Fail_When_Invalid_Section_Present()
    {
        var request = BaseRequest();
        request.CarbonInputsV2["bad_section"] = new();

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_INPUT_SECTION");
    }

    [Fact]
    public async Task Should_Fail_With_MISSING_ASSET_SECTIONS_When_All_Asset_Sections_Empty()
    {
        var request = BaseRequest();

        request.CarbonInputsV2 = new Dictionary<string, Dictionary<string, CCKpiValueRequest>>
        {
            ["chp_totals"] = new(), // empty
            ["hpm_totals"] = new(), // empty
            ["blr_totals"] = new()  // empty
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.ErrorCode == "MISSING_ASSET_SECTIONS");
    }

    [Fact]
    public async Task Should_Fail_When_Chp_Mandatory_Kpi_Missing()
    {
        var request = BaseRequest();
        request.CarbonInputsV2["chp_totals"].Remove("EC-DATA-53");

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_MANDATORY_CARBON_KPI");
    }

    [Fact]
    public async Task Should_Fail_When_Chp_Numeric_Kpi_Invalid()
    {
        var request = BaseRequest();
        request.CarbonInputsV2["chp_totals"]["EC-DATA-55"].Value = J(-1);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_NUMERIC_VALUE");
    }

    [Fact]
    public async Task Should_Fail_When_Chp_Date_Invalid()
    {
        var request = BaseRequest();
        request.CarbonInputsV2["chp_totals"]["EC-DATA-52"].Value = J("05-2026");

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_DATE_FORMAT");
    }

    [Fact]
    public async Task Should_Fail_When_Chp_Has_Unexpected_Key()
    {
        var request = BaseRequest();
        request.CarbonInputsV2["chp_totals"]["BAD-KPI"] = new() { Value = J(1) };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_CARBON_KEY");
    }

    [Fact]
    public async Task Should_Fail_When_Hpm_Invalid_Key_Present()
    {
        var request = BaseRequest();

        request.CarbonInputsV2["hpm_totals"] = new()
        {
            ["EC-DATA-66"] = new() { Value = J(100) },
            ["BAD-KPI"] = new() { Value = J(1) }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_CARBON_KEY");
    }

    [Fact]
    public async Task Should_Fail_When_Blr_Numeric_Invalid()
    {
        var request = BaseRequest();

        request.CarbonInputsV2["blr_totals"] = new()
        {
            ["EC-DATA-84"] = new() { Value = J(-100) },
            ["EC-DATA-86"] = new() { Value = J(100) }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_NUMERIC_VALUE");
    }

    [Fact]
    public async Task Should_Pass_When_All_Carbon_Inputs_Valid()
    {
        var request = BaseRequest();

        request.CarbonInputsV2["hpm_totals"] = new()
        {
            ["EC-DATA-66"] = new() { Value = J(1000) },
            ["EC-DATA-68"] = new() { Value = J(1000) }
        };

        request.CarbonInputsV2["blr_totals"] = new()
        {
            ["EC-DATA-84"] = new() { Value = J(1000) },
            ["EC-DATA-86"] = new() { Value = J(1000) }
        };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}