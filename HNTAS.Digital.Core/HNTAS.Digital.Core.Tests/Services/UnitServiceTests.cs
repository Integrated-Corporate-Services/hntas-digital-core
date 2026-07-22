using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Services;
using Microsoft.Extensions.Options;

namespace HNTAS.Digital.Core.Tests.Services
{
    public class UnitServiceTests
    {
        [Fact]
        public void GetUnit_ShouldReturnUnit_WhenKpiExists()
        {
            // Arrange
            var settings = new UnitSettings
            {
                Units =
                [
                    new KpiUnit
                    {
                        KpiId = "CC-KPI-07",
                        Unit = "°C"
                    }
                ]
            };

            var options = Options.Create(settings);
            var service = new UnitService(options);

            // Act
            var result = service.GetUnit("CC-KPI-07");

            // Assert
            Assert.Equal("°C", result);
        }

        [Fact]
        public void GetUnit_ShouldBeCaseInsensitive()
        {
            // Arrange
            var settings = new UnitSettings
            {
                Units =
                [
                    new KpiUnit
                    {
                        KpiId = "DD-KPI-10",
                        Unit = "m³/h"
                    }
                ]
            };

            var options = Options.Create(settings);
            var service = new UnitService(options);

            // Act
            var result = service.GetUnit("dd-kpi-10");

            // Assert
            Assert.Equal("m³/h", result);
        }

        [Fact]
        public void GetUnit_ShouldReturnNull_WhenKpiDoesNotExist()
        {
            // Arrange
            var settings = new UnitSettings
            {
                Units =
                [
                        new KpiUnit
                    {
                        KpiId = "CC-KPI-07",
                        Unit = "°C"
                    }
                ]
            };

            var options = Options.Create(settings);
            var service = new UnitService(options);

            // Act
            var result = service.GetUnit("UNKNOWN-KPI");

            // Assert
            Assert.Null(result);
        }
    }
}


