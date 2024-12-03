namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class SendEmailRequest
{
    public required string[] Recipients { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public string? FromDisplayName { get; set; }
}

public class SendEmailWithTemplateRequest
{
    public required string Subject { get; set; }

    public required string[] Recipients { get; set; }

    /// <summary>
    ///     Specify the content that is to be displayed in the mail
    /// </summary>
    public required MailBody MailBody { get; set; }
}

public class MailBody
{
    /// <summary>
    ///     The main content in the mail placed between the header and footer
    /// </summary>
    public required string HtmlContent { get; set; }

    /// <summary>
    ///     Optional. If not specified, the footer template will be used
    /// </summary>
    public string? HtmlFooter { get; set; }

    /// <summary>
    ///     Optional. A text that is displayed inside the header. Will default to 'Mail from Fusion'
    /// </summary>
    public string? HeaderTitle { get; set; }
}