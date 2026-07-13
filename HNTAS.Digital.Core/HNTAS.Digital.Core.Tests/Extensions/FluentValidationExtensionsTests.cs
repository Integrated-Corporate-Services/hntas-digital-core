using FluentValidation;
using HNTAS.Core.Api.Validators.Extensions;
using HNTAS.Digital.Core.Tests.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HNTAS.Digital.Core.Tests.Extensions
{
    public class FluentValidationExtensionsTests
    {
        public FluentValidationExtensionsTests()
        {
            // IMPORTANT: reset global state before each test
            ValidatorOptions.Global.PropertyNameResolver = null;
        }

        [Fact]
        public void UseJsonPropertyNames_ShouldUseJsonPropertyNameAttribute_WhenPresent()
        {
            // Arrange
            var services = new ServiceCollection();
            services.UseJsonPropertyNames();

            var propertyInfo = typeof(TestModel)
                .GetProperty(nameof(TestModel.WithJsonName));

            // Act
            var resolvedName = ValidatorOptions.Global.PropertyNameResolver(
                typeof(TestModel),
                propertyInfo,
                null);

            // Assert
            Assert.Equal("json_name", resolvedName);
        }

        [Fact]
        public void UseJsonPropertyNames_ShouldFallbackToPropertyName_WhenAttributeMissing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.UseJsonPropertyNames();

            var propertyInfo = typeof(TestModel)
                .GetProperty(nameof(TestModel.WithoutJsonName));

            // Act
            var resolvedName = ValidatorOptions.Global.PropertyNameResolver(
                typeof(TestModel),
                propertyInfo,
                null);

            // Assert
            Assert.Equal("WithoutJsonName", resolvedName);
        }

        [Fact]
        public void UseJsonPropertyNames_ShouldReturnNull_WhenMemberInfoIsNull()
        {
            // Arrange
            var services = new ServiceCollection();
            services.UseJsonPropertyNames();

            // Act
            var resolvedName = ValidatorOptions.Global.PropertyNameResolver(
                typeof(TestModel),
                null,
                null);

            // Assert
            Assert.Null(resolvedName);
        }
    }
}
