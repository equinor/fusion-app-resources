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
    }
}
