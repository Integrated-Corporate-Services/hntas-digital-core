using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Validators.Arms;
using Microsoft.Extensions.Logging;
using Moq;

namespace HNTAS.Core.Api.Tests.Validators
{
    public class CarbonCalculatorRuleValidationTests
    {
        private readonly Mock<IArmsKpiService> _armsKpiServiceMock;
        private readonly Mock<ILogger<CarbonCalculatorRuleValidation>> _loggerMock;
        private readonly CarbonCalculatorRuleValidation _validator;

        public CarbonCalculatorRuleValidationTests()
        {
            _armsKpiServiceMock = new Mock<IArmsKpiService>();
            _loggerMock = new Mock<ILogger<CarbonCalculatorRuleValidation>>();

            _validator = new CarbonCalculatorRuleValidation(
                _armsKpiServiceMock.Object,
                _loggerMock.Object
            );
        }

        // Submission aligned with your payload
        private KpiSubmission CreateSubmission()
        {
            return new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET1",
                    PeriodStart = "2026-02"
                },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },
                CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>>
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-53"] = new() { Value = 100 },
                        ["EC-DATA-55"] = new() { Value = 100 },
                        ["EC-DATA-57"] = new() { Value = 1000 },
                        ["EC-DATA-52"] = new() { Value = "2026-05-29" } // parsing fail
                    },
                    ["hpm_totals"] = new()
                    {
                        ["EC-DATA-66"] = new() { Value = 1000 },
                        ["EC-DATA-68"] = new() { Value = 1000 }
                    },
                    ["blr_totals"] = new()
                    {
                        ["EC-DATA-84"] = new() { Value = 1000 },
                        ["EC-DATA-86"] = new() { Value = 1000 }
                    }
                }
            };
        }

        // ✅ Config using your real rules
        private KpiConfiguration CreateConfig()
        {
            return new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-53"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 100 }
                        },
                        ["EC-DATA-55"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 100 }
                        },
                        ["EC-DATA-57"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 1000 }
                        },
                        ["EC-DATA-66"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 1000 }
                        },
                        ["EC-DATA-68"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 1000 }
                        },
                        ["EC-DATA-84"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 1000 }
                        },
                        ["EC-DATA-86"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 1000 }
                        },
                        // Optional: include EC-DATA-52 to validate parsing
                        ["EC-DATA-52"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000,
                            ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 100 }
                        }
                    }
                }
            };
        }

        [Fact]
        public async Task ValidateAsync_ShouldPass_WhenAllThresholdsMet()
        {
            var submission = CreateSubmission();

            // fix parsing issue
            submission.CarbonCalculatorInputs["chp_totals"]["EC-DATA-52"].Value = 101;

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateConfig());

            var result = await _validator.ValidateAsync(submission);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenThresholdNotMet()
        {
            var submission = CreateSubmission();

            // Force threshold failure: should be >=100
            submission.CarbonCalculatorInputs["chp_totals"]["EC-DATA-53"].Value = 50;

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateConfig());

            var result = await _validator.ValidateAsync(submission);

            Assert.False(result.IsValid);

            Assert.Contains(result.Errors, e =>
                e.Code == "INVALID_CARBON_INPUT_VALUE" &&
                e.Kpis.Contains("EC-DATA-53"));
        }

        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenParsingFails()
        {
            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateConfig());

            var result = await _validator.ValidateAsync(CreateSubmission());

            Assert.False(result.IsValid);

            Assert.Contains(result.Errors, e =>
                e.Code == "INVALID_CARBON_INPUT_VALUE" &&
                e.Kpis.Contains("EC-DATA-52"));
        }

        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenBelowThresholdBoundary()
        {
            var submission = CreateSubmission();

            // EC-DATA-57 requires >=1000
            submission.CarbonCalculatorInputs["chp_totals"]["EC-DATA-57"].Value = 999;

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateConfig());

            var result = await _validator.ValidateAsync(submission);

            Assert.False(result.IsValid);

            Assert.Contains(result.Errors, e =>
                e.Code == "INVALID_CARBON_INPUT_VALUE" &&
                e.Kpis.Contains("EC-DATA-57"));
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturn404_WhenConfigMissing()
        {
            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync((KpiConfiguration)null);

            var result = await _validator.ValidateAsync(CreateSubmission());

            Assert.False(result.IsValid);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturn500_WhenCarbonCalculatorRulesAreMissing()
        {
            // Arrange
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET1",
                    PeriodStart = "2026-02"
                },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },
                //  MUST be non-null to enter the block
                CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>>
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-53"] = new() { Value = 100 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>()
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            // Act
            var result = await _validator.ValidateAsync(submission);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(500, result.StatusCode);

            Assert.Contains(result.Errors, e =>
                e.Code == "CONFIGURATION_ERROR" &&
                e.Message.Contains("Carbon Calculator validation rules"));
        }


        [Fact]
        public async Task ValidateAsync_ShouldReturn200_WhenCarbonCalculatorRulesAreMissingButNoEnergyCentre()
        {
            // Arrange
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET1",
                    PeriodStart = "2026-02"
                },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.Substation } },
                //  MUST be non-null to enter the block
                CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>>
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-53"] = new() { Value = 100 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>()
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            // Act
            var result = await _validator.ValidateAsync(submission);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(200, result.StatusCode);
        }


        [Fact]
        public async Task ValidateAsync_ShouldAddKpiToOutsideLimitBucket_WhenValueExceedsUpperLimit()
        {
            // Arrange
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET1",
                    PeriodStart = "2026-02"
                },
                Elements = new List<NetworkElement> { new NetworkElement{ ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },

                CarbonCalculatorInputs = new Dictionary<string, Dictionary<string, CCKpiValue>>
                {
                    ["chp_totals"] = new()
                    {
                        // numeric, but OUTSIDE limit
                        ["EC-DATA-57"] = new() { Value = 1500000 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-57"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000000, // important
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "gte",
                                Value = 1000
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            // Act
            var result = await _validator.ValidateAsync(submission);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(400, result.StatusCode);

            Assert.Contains(result.Errors, e =>
                e.Code == "CARBON_INPUT_OUTSIDE_LIMITS" &&
                e.Kpis.Contains("EC-DATA-57"));
        }

        [Fact]
        public async Task Assess_ShouldReturnPass_WhenThresholdRuleIsNull()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "NET1",
                    PeriodStart = "2026-02"
                },
                CarbonCalculatorInputs = new()
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-99"] = new() { Value = 500 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-99"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000,
                            ThresholdRule = null //  KEY
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task Assess_ShouldHandleLteThreshold()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "NET1", PeriodStart = "2026-02" },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },
                CarbonCalculatorInputs = new()
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-101"] = new() { Value = 50 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-101"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 100,
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "lte",
                                Value = 60
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_ShouldPass_WhenPlusMinusWithinRange()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "NET1", PeriodStart = "2026-02" },
                CarbonCalculatorInputs = new()
                {
                    ["chp_totals"] = new()
                    {
                        // Target = 100, Delta = 10 → valid range [90,110]
                        ["EC-DATA-PLUS"] = new() { Value = 105 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-PLUS"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000,
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "plus_minus",
                                Target = 100,
                                Delta = 10
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.True(result.IsValid);
        }


        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenPlusMinusOutsideRange()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "NET1", PeriodStart = "2026-02" },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },
                CarbonCalculatorInputs = new()
                {
                    // Outside [90,110]
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-PLUS"] = new() { Value = 130 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-PLUS"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000,
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "plus_minus",
                                Target = 100,
                                Delta = 10
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e =>
                e.Code == "INVALID_CARBON_INPUT_VALUE" &&
                e.Kpis.Contains("EC-DATA-PLUS"));
        }

        [Fact]
        public async Task ValidateAsync_ShouldPass_WhenEqWithinEpsilon()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "NET1", PeriodStart = "2026-02" },
                CarbonCalculatorInputs = new()
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-EQ"] = new() { Value = 100.0000005 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-EQ"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000,
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "eq",
                                Value = 100.0
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ValidateAsync_ShouldFail_WhenEqOutsideEpsilon()
        {
            var submission = new KpiSubmission
            {
                MetaData = new KpiMetadata { NetworkId = "NET1", PeriodStart = "2026-02" },
                Elements = new List<NetworkElement> { new NetworkElement { ElementId = "0001", Type = HeatNetworkElementType.EnergyCentre } },
                CarbonCalculatorInputs = new()
                {
                    ["chp_totals"] = new()
                    {
                        ["EC-DATA-EQ"] = new() { Value = 100.01 }
                    }
                }
            };

            var config = new KpiConfiguration
            {
                CarbonCalculator = new CarbonCalculatorConfig
                {
                    Rules = new Dictionary<string, KpiRule>
                    {
                        ["EC-DATA-EQ"] = new()
                        {
                            LowerLimit = 0,
                            UpperLimit = 1000,
                            ThresholdRule = new KpiThresholdRule
                            {
                                Type = "eq",
                                Value = 100.0
                            }
                        }
                    }
                }
            };

            _armsKpiServiceMock
                .Setup(x => x.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(config);

            var result = await _validator.ValidateAsync(submission);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e =>
                e.Code == "INVALID_CARBON_INPUT_VALUE" &&
                e.Kpis.Contains("EC-DATA-EQ"));
        }
    }
}
