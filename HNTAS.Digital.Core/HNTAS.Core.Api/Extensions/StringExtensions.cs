namespace HNTAS.Core.Api.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Removes carriage returns and line feeds to prevent log injection.
        /// </summary>
        public static string ToSafeLog(this string? id)
        {
            return (id ?? string.Empty)
                .Replace("\r", "")
                .Replace("\n", "");
        }

        /// <summary>
        /// Masks an email address for logging to reduce exposure of private information.
        /// </summary>
        public static string ToMaskedEmailForLog(this string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return string.Empty;
            }
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex == email.Length - 1)
            {
                return "***".ToSafeLog();
            }
            var localPart = email.Substring(0, atIndex);
            var domainPart = email.Substring(atIndex + 1);
            var maskedLocal = localPart.Length <= 1
                ? "*"
                : $"{localPart[0]}***";
            return $"{maskedLocal}@{domainPart}".ToSafeLog();
        }
    }
}
