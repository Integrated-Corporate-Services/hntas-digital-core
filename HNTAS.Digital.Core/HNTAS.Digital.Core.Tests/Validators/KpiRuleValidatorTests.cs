using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Validators.Arms;
using Microsoft.Extensions.Logging;
using Moq;


namespace HNTAS.Digital.Core.Tests.Validators
{
    public class KpiRuleValidatorTests
    {
        private readonly Mock<IArmsKpiService> _mockKpiService;
        private readonly Mock<ILogger<KpiRuleValidator>> _mockLogger;
        private readonly KpiRuleValidator _validator;

        public KpiRuleValidatorTests()
        {
            _mockKpiService = new Mock<IArmsKpiService>();
            _mockLogger = new Mock<ILogger<KpiRuleValidator>>();
            _validator = new KpiRuleValidator(_mockKpiService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ValidateAsync_WhenConfigNotFound_ReturnsFailure()
        {
            // Arrange
            var networkId = "HN1234";
            var request = CreateSubmissionStub(networkId);
            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId))
                .ReturnsAsync((KpiConfiguration)null);

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("KPI Configuration not found for this network.", result.Detail);
        }

        [Fact]
        public async Task ValidateAsync_AggregatedKpis_UpdatesAssessmentStatus()
        {
            // Arrange
            var networkId = "HN_AGG";
            var kpiKey = "TOTAL_HEAT_LOSS";
            var request = CreateSubmissionStub(networkId);
            request.ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>
            {
                { kpiKey, new KpiValueAggregated { Value = 85.0 } }
            };

            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.ConsumerConnection,
                        Kpis = new Dictionary<string, KpiRule>
                        {
                            { kpiKey, new KpiRule { LowerLimit = 0, UpperLimit = 100, ThresholdRule = new KpiThresholdRule { Type = "lte", Value = 90 } } }
                        }
                    }
                }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            await _validator.ValidateAsync(request);

            // Assert
            Assert.Equal(KPIAssessmentStatus.Pass, request.ConsumerConnectionAggregatedKpis[kpiKey].AssessmentStatus);
        }

        [Theory]
        // Case: Within limits, meets performance target (Pass)
        [InlineData(75, 0, 100, "gte", 70, KPIAssessmentStatus.Pass)]
        // Case: Within limits, fails performance target (Fail)
        [InlineData(65, 0, 100, "gte", 70, KPIAssessmentStatus.Fail)]
        // Case: Exceeds upper limit (OutsideLimit)
        [InlineData(110, 0, 100, "gte", 70, KPIAssessmentStatus.OutsideLimit)]
        // Case: Below lower limit (OutsideLimit)
        [InlineData(-5, 0, 100, "gte", 70, KPIAssessmentStatus.OutsideLimit)]
        public async Task ValidateAsync_IndividualElements_CorrectlyAssessesStatus(
            double value, double lower, double upper, string op, double threshold, KPIAssessmentStatus expected)
        {
            // Arrange
            var networkId = "HN_IND";
            var kpiId = "EFFICIENCY_KPI";
            var request = CreateSubmissionStub(networkId);
            request.Elements.Add(new NetworkElement
            {
                ElementId = "E001",
                Type = HeatNetworkElementType.EnergyCentre,
                Kpis = new Dictionary<string, KpiValue> { { kpiId, new KpiValue { Value = value } } }
            });

            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.EnergyCentre,
                        Kpis = new Dictionary<string, KpiRule>
                        {
                            { kpiId, new KpiRule { LowerLimit = lower, UpperLimit = upper, ThresholdRule = new KpiThresholdRule { Type = op, Value = threshold } } }
                        }
                    }
                }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            await _validator.ValidateAsync(request);

            // Assert
            Assert.Equal(expected, request.Elements.First().Kpis[kpiId].AssessmentStatus);
        }

        [Fact]
        public async Task ValidateAsync_PlusMinusRule_HandlesRangeCorrectly()
        {
            // Arrange
            var networkId = "HN_RANGE";
            var kpiId = "TEMP_STABILITY";
            var request = CreateSubmissionStub(networkId);
            request.Elements.Add(new NetworkElement
            {
                ElementId = "E001",
                Type = HeatNetworkElementType.Substation,
                Kpis = new Dictionary<string, KpiValue> { { kpiId, new KpiValue { Value = 72 } } }
            });

            // Target 70 with Delta 5 means Pass is 65 to 75
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.Substation,
                        Kpis = new Dictionary<string, KpiRule>
                        {
                            { kpiId, new KpiRule {
                                LowerLimit = 0, UpperLimit = 100,
                                ThresholdRule = new KpiThresholdRule { Type = "plus_minus", Target = 70, Delta = 5 }
                            }}
                        }
                    }
                }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            await _validator.ValidateAsync(request);

            // Assert
            Assert.Equal(KPIAssessmentStatus.Pass, request.Elements.First().Kpis[kpiId].AssessmentStatus);
        }


        [Fact]
        public async Task ValidateAsync_WhenNoRuleMatches_SetsStatusToUndefined()
        {
            // Arrange
            var networkId = "HN_UNDEFINED";
            var request = CreateSubmissionStub(networkId);
            request.Elements.Add(new NetworkElement
            {
                ElementId = "E001",
                Type = HeatNetworkElementType.EnergyCentre,
                Kpis = new Dictionary<string, KpiValue> { { "MISSING_KPI", new KpiValue { Value = 50 } } }
            });

            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement> { new() { Type = HeatNetworkElementType.EnergyCentre, Kpis = new() } }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            await _validator.ValidateAsync(request);

            // Assert
            Assert.Equal(KPIAssessmentStatus.Undefined, request.Elements.First().Kpis["MISSING_KPI"].AssessmentStatus);
        }


        [Fact]
        public async Task ValidateAsync_WhenMandatoryKpiIsMissing_ReturnsFailureWithDetails()
        {
            // Arrange
            var networkId = "HN_MANDATORY_FAIL";
            var mandatoryKpiId = "CD-KPI-01";
            var request = CreateSubmissionStub(networkId);

            // We send an element but leave the Kpis dictionary empty (missing the mandatory one)
            request.Elements.Add(new NetworkElement
            {
                ElementId = "E001",
                Type = HeatNetworkElementType.ConsumerConnection,
                Kpis = new Dictionary<string, KpiValue>()
            });

            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.ConsumerConnection,
                        Kpis = new Dictionary<string, KpiRule>
                        {
                            // Rule specifies this KPI is Mandatory
                            { mandatoryKpiId, new KpiRule { IsMandatory = true, LowerLimit = 0, UpperLimit = 100 } }
                        }
                    }
                }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains(result.Errors, e => e.Code == "MISSING_MANDATORY_KPI" && e.ElementId == "E001");
        }



        [Fact]
        public async Task ValidateAsync_WhenMandatoryKpisArePresent_ReturnsSuccess()
        {
            // Arrange
            var networkId = "HN_MANDATORY_PASS";
            var kpiId = "CD-KPI-01";
            var request = CreateSubmissionStub(networkId);

            request.Elements.Add(new NetworkElement
            {
                ElementId = "E001",
                Type = HeatNetworkElementType.ConsumerConnection,
                Kpis = new Dictionary<string, KpiValue> { { kpiId, new KpiValue { Value = 50 } } }
            });

            var config = new KpiConfiguration
            {
                NetworkId = networkId,
                Elements = new List<KpiNetworkElement>
                {
                    new()
                    {
                        Type = HeatNetworkElementType.ConsumerConnection,
                        Kpis = new Dictionary<string, KpiRule>
                        {
                            { kpiId, new KpiRule { IsMandatory = true, LowerLimit = 0, UpperLimit = 100 } }
                        }
                    }
                }
            };

            _mockKpiService.Setup(s => s.GetConfigurationAsync(networkId)).ReturnsAsync(config);

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.Errors == null || !result.Errors.Any());
        }

        // --- Helper Methods ---

        private KpiSubmission CreateSubmissionStub(string networkId)
        {
            return new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = networkId,
                    PeriodStart = "2026-04-01T00:00:00Z",
                    SourceSystem = "TestSystem"
                },
                CreatedAt = DateTime.UtcNow,
                ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                Elements = new List<NetworkElement>()
            };
        }
    }
}