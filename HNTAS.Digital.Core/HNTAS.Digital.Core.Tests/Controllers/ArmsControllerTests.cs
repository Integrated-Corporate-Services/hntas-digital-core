using AutoMapper;
using FluentValidation;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using HNTAS.Core.Api.Services;
using HNTAS.Core.Api.Validators.Arms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HNTAS.Digital.Core.Tests.Controllers
{
    public class ArmsControllerTests
    {
        private readonly Mock<IArmsKpiService> _mockArmsKpiService;
        private readonly Mock<ILogger<ArmsController>> _mockLogger;
        private readonly Mock<IValidator<KpiSubmissionRequest>> _mockKpiSubmissionRequestValidator;
        private readonly Mock<IValidator<KpiSubmissionRequestV2>> _mockKpiSubmissionRequestValidatorV2;
        private readonly Mock<IHeatNetworkValidator> _mockHeatNetworkValidator;
        private readonly Mock<IKpiRuleValidator> _mockKpiRuleValidator;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICarbonCalculatorService> _mockCarbonCalculatorService;
        private readonly Mock<ISubmissionCCService> _mockSubmissionCarbonCalculator;
        private readonly Mock<ICarbonCalculatorRuleValidation> _mockCarbonCalculatorRuleValidation;
        private readonly Mock<IOptions<ArmsSettings>> _mockArmsSettings;
        private readonly ArmsController _sut;

        public ArmsControllerTests()
        {
            _mockArmsKpiService = new Mock<IArmsKpiService>();
            _mockLogger = new Mock<ILogger<ArmsController>>();
            _mockKpiSubmissionRequestValidator = new Mock<IValidator<KpiSubmissionRequest>>();
            _mockKpiSubmissionRequestValidatorV2 = new Mock<IValidator<KpiSubmissionRequestV2>>();
            _mockHeatNetworkValidator = new Mock<IHeatNetworkValidator>();
            _mockKpiRuleValidator = new Mock<IKpiRuleValidator>();
            _mockMapper = new Mock<IMapper>();
            _mockCarbonCalculatorService = new Mock<ICarbonCalculatorService>();
            _mockSubmissionCarbonCalculator = new Mock<ISubmissionCCService>();
            _mockCarbonCalculatorRuleValidation = new Mock<ICarbonCalculatorRuleValidation>();
            _mockArmsSettings = new Mock<IOptions<ArmsSettings>>();

            // Setup default ArmsSettings before controller instantiation
            _mockArmsSettings.Setup(x => x.Value).Returns(new ArmsSettings
            {
                EnableExtendedValidation = true
            });

            _sut = new ArmsController(
                _mockArmsKpiService.Object,
                _mockLogger.Object,
                _mockKpiSubmissionRequestValidator.Object,
                _mockKpiSubmissionRequestValidatorV2.Object,
                _mockMapper.Object,
                 _mockArmsSettings.Object,
                _mockHeatNetworkValidator.Object,
                _mockKpiRuleValidator.Object,
                _mockCarbonCalculatorService.Object,
                _mockSubmissionCarbonCalculator.Object,
                _mockCarbonCalculatorRuleValidation.Object
            );
        }

        [Fact]
        public async Task SubmitKpi_ShouldReturnOk_WhenValidRequest()
        {
            // Arrange
            var request = new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequest>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                    Elements = new List<NetworkElement>()
                    {
                        new NetworkElement
                        {
                            ElementId = "element-1",                            
                            Type = HeatNetworkElementType.EnergyCentre,
                            Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            }
                        }
                    }
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidator.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult("test"));

            // Act
            var result = await _sut.SubmitKpis(request);
            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitKpi_ShouldThrowMongoException()
        {
            // Arrange
            var request = new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequest>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidator.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new MongoDB.Driver.MongoException("MongoDB error"));

            // Act
            var result = await _sut.SubmitKpis(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
            Assert.Equal(503, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpi_ShouldThrowException()
        {
            // Arrange
            var request = new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequest>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidator.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _sut.SubmitKpis(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpi_BadRequest()
        {
            // Arrange
            var request = new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequest>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            // set validationResult to be invalid
            validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("MetaData.NetworkId", "NetworkId is required"));

            _mockKpiSubmissionRequestValidator.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _sut.SubmitKpis(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpi_CreateProblem()
        {
            // Arrange
            var request = new KpiSubmissionRequest
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequest>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                    Elements = new List<NetworkElement>()
                    {
                        new NetworkElement
                        {
                            ElementId = "element-1",
                            Type = HeatNetworkElementType.EnergyCentre,
                            Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            }
                        }
                    }
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidator.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    false,
                    null,
                    "test",
                    0,
                    null
                )));            

            // Act
            var result = await _sut.SubmitKpis(request);
            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(0, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpisV2_ShouldReturnOk_WhenValidRequest()
        {
            // Arrange
            var request = new KpiSubmissionRequestV2
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequestV2>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                    Elements = new List<NetworkElement>()
                    {
                        new NetworkElement
                        {
                            ElementId = "element-1",
                            Type = HeatNetworkElementType.EnergyCentre,
                            Kpis = new Dictionary<string, KpiValue>()
                            {
                                { "kpi-1", new KpiValue { Value = 10, AssessmentStatus = KPIAssessmentStatus.OutsideLimit } }
                            }
                        }
                    }
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidatorV2.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockCarbonCalculatorRuleValidation.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult("test"));

            // Act
            var result = await _sut.SubmitKpisV2(request);
            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        }

        [Fact]
        public async Task SubmitKpisV2_ShouldThrowMongoException()
        {
            // Arrange
            var request = new KpiSubmissionRequestV2
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequestV2>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidatorV2.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockCarbonCalculatorRuleValidation.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new MongoDB.Driver.MongoException("MongoDB error"));

            // Act
            var result = await _sut.SubmitKpisV2(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
            Assert.Equal(503, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpisV2_ShouldThrowException()
        {
            // Arrange
            var request = new KpiSubmissionRequestV2
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequestV2>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            _mockKpiSubmissionRequestValidatorV2.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
            Assert.True(validationResult.IsValid);

            _mockCarbonCalculatorRuleValidation.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockHeatNetworkValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<IEnumerable<NetworkElementRequest>>()))
            .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                true,
                null,
                "test",
                0,
                null
            )));

            _mockKpiRuleValidator.Setup(v => v.ValidateAsync(It.IsAny<KpiSubmission>()))
                .Returns(Task.FromResult(new HNTAS.Core.Api.Common.ValidationGateResult(
                    true,
                    null,
                    "test",
                    0,
                    null
                )));

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _sut.SubmitKpisV2(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task SubmitKpisV2_BadRequest()
        {
            // Arrange
            var request = new KpiSubmissionRequestV2
            {
                MetaData = new KpiMetadata
                {
                    NetworkId = "test-network-id",
                    PeriodStart = "2024-01-01",
                },
            };

            _mockMapper.Setup(m => m.Map<KpiSubmission>(It.IsAny<KpiSubmissionRequestV2>()))
                .Returns(new KpiSubmission
                {
                    MetaData = new KpiMetadata
                    {
                        NetworkId = "test-network-id",
                        PeriodStart = "2024-01-01",
                    },
                    ConsumerConnectionAggregatedKpis = new Dictionary<string, KpiValueAggregated>(),
                });

            var validationResult = new FluentValidation.Results.ValidationResult();
            validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("MetaData.NetworkId", "NetworkId is required"));
            _mockKpiSubmissionRequestValidatorV2.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);

            _mockArmsKpiService.Setup(v => v.CreateOrUpdateSubmissionAsync(It.IsAny<KpiSubmission>()))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _sut.SubmitKpisV2(request);
            // Assert
            var objectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfig_ShouldReturnOk_WhenConfigExists()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,                
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponse>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponse { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .ReturnsAsync(config);
            // Act
            var result = await _sut.GetKpiConfig(networkId);
            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiConfig_ShouldThrowException()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponse>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponse { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .ThrowsAsync(new Exception());
            // Act
            var result = await _sut.GetKpiConfig(networkId);
            // Assert
            Assert.Equal(500, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfig_BadRequest()
        {
            // Arrange
            var networkId = "test";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };            
            // Act
            var result = await _sut.GetKpiConfig(networkId);
            // Assert
            Assert.Equal(400, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfig_NotFound()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponse>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponse { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .Returns(Task.FromResult<KpiConfiguration>(null));
            // Act
            var result = await _sut.GetKpiConfig(networkId);
            // Assert
            Assert.Equal(404, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfigV2_ShouldReturnOk_WhenConfigExists()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponseV2>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponseV2 { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .ReturnsAsync(config);
            // Act
            var result = await _sut.GetKpiConfigV2(networkId);
            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetKpiConfigV2_ShouldThrowException()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponseV2>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponseV2 { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .ThrowsAsync(new Exception());
            // Act
            var result = await _sut.GetKpiConfigV2(networkId);
            // Assert
            Assert.Equal(500, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfigV2_BadRequest()
        {
            // Arrange
            var networkId = "test";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            // Act
            var result = await _sut.GetKpiConfigV2(networkId);
            // Assert
            Assert.Equal(400, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task GetKpiConfigV2_NotFound()
        {
            // Arrange
            var networkId = "HN1000001";
            var config = new KpiConfiguration
            {
                NetworkId = networkId,
            };
            _mockMapper.Setup(m => m.Map<KpiConfigResponseV2>(It.IsAny<KpiConfiguration>()))
                .Returns(new KpiConfigResponseV2 { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.GetConfigurationAsync(networkId))
                .Returns(Task.FromResult<KpiConfiguration>(null));
            // Act
            var result = await _sut.GetKpiConfigV2(networkId);
            // Assert
            Assert.Equal(404, (result.Result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task SaveConfig_ShouldReturnOk()
        {
            // Arrange
            var request = new KpiConfigRequest
            {
                NetworkId = "HN1000001",
            };
            
            _mockMapper.Setup(m => m.Map<KpiConfiguration>(It.IsAny<KpiConfigRequest>()))
                .Returns(new KpiConfiguration { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.CreateOrUpdateConfigurationAsync(It.IsAny<KpiConfiguration>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await _sut.SaveConfig(request);
            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SaveConfig_ThrowException()
        {
            // Arrange
            var request = new KpiConfigRequest
            {
                NetworkId = "HN1000001",
            };

            _mockMapper.Setup(m => m.Map<KpiConfiguration>(It.IsAny<KpiConfigRequest>()))
                .Returns(new KpiConfiguration { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.CreateOrUpdateConfigurationAsync(It.IsAny<KpiConfiguration>()))
                .ThrowsAsync(new Exception());
            // Act
            var result = await _sut.SaveConfig(request);
            // Assert
            Assert.Equal(500, (result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task SaveConfig_BadRequest()
        {
            // Arrange
            var request = new KpiConfigRequest
            {
                NetworkId = "",
            };
            
            // Act
            var result = await _sut.SaveConfig(request);
            // Assert
            Assert.Equal(400, (result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task SaveConfigV2_ShouldReturnOk()
        {
            // Arrange
            var request = new KpiConfigRequestV2
            {
                NetworkId = "HN1000001",
            };

            _mockMapper.Setup(m => m.Map<KpiConfiguration>(It.IsAny<KpiConfigRequest>()))
                .Returns(new KpiConfiguration { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.CreateOrUpdateConfigurationAsync(It.IsAny<KpiConfiguration>()))
                .Returns(Task.CompletedTask);
            // Act
            var result = await _sut.SaveConfigV2(request);
            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task SaveConfigV2_ThrowException()
        {
            // Arrange
            var request = new KpiConfigRequestV2
            {
                NetworkId = "HN1000001",
            };

            _mockMapper.Setup(m => m.Map<KpiConfiguration>(It.IsAny<KpiConfigRequest>()))
                .Returns(new KpiConfiguration { NetworkId = "HN1000001" });
            _mockArmsKpiService.Setup(v => v.CreateOrUpdateConfigurationAsync(It.IsAny<KpiConfiguration>()))
                .ThrowsAsync(new Exception());
            // Act
            var result = await _sut.SaveConfigV2(request);
            // Assert
            Assert.Equal(500, (result as ObjectResult)?.StatusCode);
        }

        [Fact]
        public async Task SaveConfigV2_BadRequest()
        {
            // Arrange
            var request = new KpiConfigRequestV2
            {
                NetworkId = "",
            };

            // Act
            var result = await _sut.SaveConfigV2(request);
            // Assert
            Assert.Equal(400, (result as ObjectResult)?.StatusCode);
        }
    }
}
