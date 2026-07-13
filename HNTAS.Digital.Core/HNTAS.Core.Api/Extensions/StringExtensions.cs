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
    }
}
