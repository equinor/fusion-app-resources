using System;
using System.Net.Http;

namespace Fusion.Resources.Application.Summary;

public class SummaryIntegrationException : Exception
{
    public SummaryIntegrationException(string message, HttpResponseMessage response, string content) : base(message)
    {
        Response = response;
        Content = content;
    }

    public HttpResponseMessage Response { get; }
    public string Content { get; }
}