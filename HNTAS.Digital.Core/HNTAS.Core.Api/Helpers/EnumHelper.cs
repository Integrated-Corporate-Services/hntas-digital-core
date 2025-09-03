using HNTAS.Core.Api.Models;
using System.ComponentModel;
using System.Reflection;

namespace HNTAS.Core.Api.Helpers
{
    public static class EnumHelper
    {
        public static List<EnumItemResponse> GetEnumItems<T>() where T : Enum
        {
            var type = typeof(T);
            return Enum.GetValues(type)
                .Cast<T>()
                .Select(e => new EnumItemResponse
                {
                    Value = Convert.ToInt32(e),
                    Name = e.ToString(),
                    Description = type
                        .GetMember(e.ToString())
                        .First()
                        .GetCustomAttribute<DescriptionAttribute>()?.Description ?? e.ToString()
                })
                .ToList();
        }
    }
}
