using HNTAS.Core.Api.Data.Models;
using System.Text.RegularExpressions;

namespace HNTAS.Core.Api.Helpers
{
    public class StringFormatter
    {
        public static string ToTitleCaseSingleWord(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            // Convert the first character to uppercase and the rest to lowercase.
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        public static string FormatAddress(RegisteredAddress? address)
        {
            if (address == null)
            {
                return "";
            }

            var parts = new List<string?>
            {
                address.AddressLine1,
                address.AddressLine2,
                address.Town,
                address.County,
                address.Country,
                address.Postcode
            };

            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        public static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove control characters (including newlines, tabs, etc.) to prevent log forging
            string sanitized = Regex.Replace(input, @"[\x00-\x1F\x7F]", string.Empty);

            // Escape curly braces for logging frameworks that use them as format delimiters
            sanitized = sanitized.Replace("{", "{{").Replace("}", "}}");

            // Remove non-alphanumeric characters
            sanitized = Regex.Replace(sanitized, @"[^A-Za-z0-9]", string.Empty);

            // If input had content but sanitization removed everything, flag it
            return sanitized.Length == 0 && input.Length > 0 ? "[INVALID_USER_INPUT]" : sanitized;
        }
    }
}
