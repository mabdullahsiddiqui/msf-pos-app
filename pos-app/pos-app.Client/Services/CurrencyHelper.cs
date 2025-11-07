using System.Globalization;

namespace pos_app.Client.Services
{
    public static class CurrencyHelper
    {
        /// <summary>
        /// Formats a decimal amount as Pakistani Rupee currency
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted string with Rs prefix (e.g., "Rs 1,234.56")</returns>
        public static string FormatCurrency(decimal amount)
        {
            return $"Rs {amount:N2}";
        }

        /// <summary>
        /// Formats a decimal amount as Pakistani Rupee currency with custom format
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <param name="format">Custom format string (default: "N2")</param>
        /// <returns>Formatted string with Rs prefix</returns>
        public static string FormatCurrency(decimal amount, string format)
        {
            return $"Rs {amount.ToString(format)}";
        }

        /// <summary>
        /// Formats a decimal amount as number with 2 decimal places
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted string (e.g., "1,234.56")</returns>
        public static string FormatNumber(decimal amount)
        {
            return amount.ToString("N2");
        }

        /// <summary>
        /// Formats a decimal amount as number with custom format
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <param name="format">Custom format string (default: "N2")</param>
        /// <returns>Formatted string</returns>
        public static string FormatNumber(decimal amount, string format)
        {
            return amount.ToString(format);
        }

        /// <summary>
        /// Formats a nullable decimal amount as Pakistani Rupee currency
        /// </summary>
        /// <param name="amount">The nullable amount to format</param>
        /// <returns>Formatted string with Rs prefix or "Rs 0.00" if null</returns>
        public static string FormatCurrency(decimal? amount)
        {
            return amount.HasValue ? FormatCurrency(amount.Value) : "Rs 0.00";
        }

        /// <summary>
        /// Formats a nullable decimal amount as number
        /// </summary>
        /// <param name="amount">The nullable amount to format</param>
        /// <returns>Formatted string or "0.00" if null</returns>
        public static string FormatNumber(decimal? amount)
        {
            return amount.HasValue ? FormatNumber(amount.Value) : "0.00";
        }
    }
}
