using FluentValidation;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Validators.Extensions
{
    public static class FluentValidationExtensions
    {
        public static void UseJsonPropertyNames(this IServiceCollection services)
        {
            ValidatorOptions.Global.PropertyNameResolver = (type, memberInfo, expression) =>
            {
                if (memberInfo != null)
                {
                    // Look for [JsonPropertyName] on the property
                    var attribute = memberInfo.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                              .FirstOrDefault() as JsonPropertyNameAttribute;

                    return attribute?.Name ?? memberInfo.Name;
                }
                return null;
            };
        }
    }
}
