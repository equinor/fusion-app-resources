using System.Text.RegularExpressions;

namespace Fusion.Resources.Api
{
    public static class StringExtensions
    {
        public static string? TrimText(this string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (text.Length <= maxLength)
                return text;

            return text[..maxLength] + "...";

        }

        /// <summary>
        /// Checks if the string only consist of numbers.
        /// </summary>
        public static bool IsNumbers(this string? text)
        {
            return Regex.IsMatch(text ?? "", @"\d+");
        }
    }
}
