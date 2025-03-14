#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class RequestHeadersScope : IDisposable
{
    internal static AsyncLocal<RequestHeadersScope?> Current = new AsyncLocal<RequestHeadersScope?>();

    public RequestHeadersScope()
    {
        Current.Value = this;
    }

    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public RequestHeadersScope WithChangeSource(string source, string? sourceId = null)
    {
        if (string.IsNullOrEmpty(sourceId))
            Headers["x-fusion-change-source"] = $"{source}";
        else
            Headers["x-fusion-change-source"] = $"{source}; {sourceId}";

        return this;
    }

    public RequestHeadersScope WithEditMode(bool enableEditMode)
    {
        Headers["x-pro-edit-mode"] = $"{enableEditMode}";
        return this;
    }

    public RequestHeadersScope WithHeader(string key, string value)
    {
        Headers[key] = value;
        return this;
    }

    public void Dispose()
    {
        Current.Value = null;
    }
}