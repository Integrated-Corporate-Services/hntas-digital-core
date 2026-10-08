namespace HNTAS.Core.Api.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Removes carriage returns and line feeds to prevent log injection.
        /// Masks email addresses to avoid exposing private information in logs.
        /// </summary>
        public static string ToSafeLog(this string? id)
        {
            var sanitized = (id ?? string.Empty)
                .Replace("\r", "")
                .Replace("\n", "");

            var atIndex = sanitized.IndexOf('@');
            if (atIndex > 0 && atIndex < sanitized.Length - 1)
            {
                var localPart = sanitized[..atIndex];
                var domainPart = sanitized[(atIndex + 1)..];

                var maskedLocalPart = localPart.Length <= 1
                    ? "*"
                    : $"{localPart[0]}***";

                return $"{maskedLocalPart}@{domainPart}";
            }

            return sanitized;
        }
    }
}
