using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Validators.Arms;
using ElementType = HNTAS.Core.Api.Enums.HeatNetworkElementType;

namespace HNTAS.Digital.Core.Tests.Validators
{
    public class KpiElementsValidatorTests
    {
        private readonly KpiElementsValidator _validator = new KpiElementsValidator();

        private BaseKpiSubmissionRequest CreateValidRequest()
        {
            return new BaseKpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET-001",
                    PeriodStart = "2026-01",
                    SourceSystem = "TestSystem"
                },
                Elements = new List<NetworkElementRequest>
                {
                    new NetworkElementRequest
                    {
                        Type = ElementType.EnergyCentre.ToString(),
                        ElementId = "00001",
                        Kpis = new Dictionary<string, KpiValueRequest>
                        {
                            ["EC-KPI-01"] = new KpiValueRequest { Value = 99.2 },
                            ["EC-KPI-02"] = new KpiValueRequest { Value = 97.1 }
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task Should_Pass_When_Request_Is_Valid()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }


        [Fact]
        public async Task Should_Fail_When_Elements_Is_Empty()
        {
            var request = CreateValidRequest();
            request.Elements = new List<NetworkElementRequest>();

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }



        [Fact]
        public async Task Should_Fail_When_ElementId_Invalid()
        {
            var request = CreateValidRequest();
            request.Elements[0].ElementId = "123";

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_ELEMENT_ID");
        }



        [Fact]
        public async Task Should_Fail_When_Element_Type_Invalid()
        {
            var request = CreateValidRequest();
            request.Elements[0].Type = "WrongType";

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_ELEMENT_TYPE");
        }

        [Fact]
        public async Task Should_Fail_When_Invalid_Kpi_For_Element()
        {
            var request = CreateValidRequest();

            request.Elements[0].Kpis["DD-KPI-01"] = new KpiValueRequest() { Value = 100 };

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_KPI_FOR_TYPE");
        }

        [Fact]
        public async Task Should_Group_Invalid_Kpis()
        {
            var request = CreateValidRequest();

            request.Elements[0].Kpis.Clear();
            request.Elements[0].Kpis["BAD-1"] = new KpiValueRequest { Value = 1 };
            request.Elements[0].Kpis["BAD-2"] = new KpiValueRequest { Value = 2 };


            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);

            var error = result.Errors.First(e => e.ErrorCode == "INVALID_KPI_FOR_TYPE");

            dynamic state = error.CustomState;

            Assert.NotNull(state);

            var kpisProp = state.GetType().GetProperty("kpis");
            var kpis = (List<string>)kpisProp.GetValue(state);

            Assert.NotNull(kpis);
            Assert.Equal(2, kpis.Count);
        }

        [Fact]
        public async Task Should_Fail_When_Aggregated_Data_Provided_Without_ConsumerConnection()
        {
            var request = CreateValidRequest();

            request.ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregatedRequest>
            {
                ["CC-KPI-01"] = new KpiValueAggregatedRequest()
            };

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "UNEXPECTED_AGGREGATED_DATA");
        }

        [Fact]
        public async Task Should_Fail_When_Missing_Mandatory_Aggregated_Kpis()
        {
            var request = new BaseKpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET-001",
                    PeriodStart = "2026-01",
                    SourceSystem = "TestSystem"
                },
                Elements = new List<NetworkElementRequest>
                {
                    new NetworkElementRequest
                    {
                        Type = ElementType.ConsumerConnection.ToString(),
                        ElementId = "00002",
                        Kpis = new Dictionary<string, KpiValueRequest>
                        {
                            ["CC-KPI-04"] = new KpiValueRequest { Value = 1}
                        }
                    }
                },
                ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregatedRequest>
                {
                    ["CC-KPI-01"] = new()
                }
            };

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_MANDATORY_KPI");
        }

        [Fact]
        public async Task Should_Fail_When_Invalid_Aggregated_Kpis()
        {
            var request = new BaseKpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET-001",
                    PeriodStart = "2026-01",
                    SourceSystem = "TestSystem"
                },
                Elements = new List<NetworkElementRequest>
                {
                    new NetworkElementRequest
                    {
                        Type = ElementType.ConsumerConnection.ToString(),
                        ElementId = "00002",
                        Kpis = new Dictionary<string, KpiValueRequest>
                        {
                            ["CC-KPI-04"] =new KpiValueRequest { Value = 1}
                        }
                    }
                },
                ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregatedRequest>
                {
                    ["CC-KPI-01"] = new(),
                    ["CC-KPI-02"] = new(),
                    ["CC-KPI-03"] = new(),
                    ["INVALID-KPI"] = new()
                }
            };

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_AGGREGATED_KPI");
        }

        [Fact]
        public async Task Should_Handle_Null_Elements()
        {
            var request = new BaseKpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET-001",
                    PeriodStart = "2026-01",
                    SourceSystem = "TestSystem"
                },
                Elements = null
            };

            var result = await _validator.ValidateAsync(request);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
    }
}
