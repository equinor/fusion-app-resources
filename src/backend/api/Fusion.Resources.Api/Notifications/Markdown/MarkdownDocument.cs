using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Notifications.Markdown
{
    public class MarkdownDocument
    {
        string mdString = string.Empty;

        public MarkdownDocument Paragraph(string content)
        {
            mdString += content + "\n\n";
            return this;
        }

        public MarkdownDocument List(Action<MdList> list)
        {
            var listBuilder = new MdList();
            list(listBuilder);

            mdString += $"{listBuilder}\n\n";
            return this;
        }

        /// <summary>
        /// Generates a hyperlink paragraph if link is provided, with spacing before and after
        /// </summary> 
        /// <param name="text">The text to display</param>
        /// <param name="url">The url. Can be null, if so the paragraph will be empty</param>
        /// <returns>Doc with updated content</returns>
        public MarkdownDocument LinkParagraph(string text, string? url)
        {
            if (url is null)
                return this;

            mdString += $"{MdToken.Newline()} {MdToken.Hyperlink(text, url)} {MdToken.Newline()}";

            return this;
        }

        public string Build() => mdString;

        public class MdList
        {
            List<string> items = new List<string>();

            public MdList ListItem(string text)
            {
                items.Add(text);
                return this;
            }

            public override string ToString()
            {
                return string.Join("\n", items.Select(i => $" - {i}"));
            }
        }
    }
}
