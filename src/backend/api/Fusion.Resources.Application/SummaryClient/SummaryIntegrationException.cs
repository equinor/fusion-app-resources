using System;

namespace Fusion.Resources.Application.SummaryClient;

public sealed class SummaryIntegrationException : Exception
{
    public SummaryIntegrationException(string message, string content) : base(message)
    {
        Data["Content"] = content;
    }

    public string Content =>
        Data["Content"]?.ToString() ?? string.Empty;
}