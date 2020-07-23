namespace Fusion.Resources.Api.Notifications.Markdown
{
    public class MdToken
    {
        public static string Bold(string? text) => text == null ? string.Empty : $"**{text.Trim()}**";
        public static string Italic(string text) => $"*{text.Trim()}*";
        public static string ListItem(string text) => $" - {text.Trim()}";
        public static string Newline() => "  \n";
    }
}
