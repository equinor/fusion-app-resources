#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class OrgClientRequestHeadersScope : IDisposable
{
    internal static AsyncLocal<OrgClientRequestHeadersScope?> Current = new AsyncLocal<OrgClientRequestHeadersScope?>();

    public OrgClientRequestHeadersScope()
    {
        Current.Value = this;
    }

    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public OrgClientRequestHeadersScope WithChangeSource(string source, string? sourceId = null)
    {
        if (string.IsNullOrEmpty(sourceId))
            Headers["x-fusion-change-source"] = $"{source}";
        else
            Headers["x-fusion-change-source"] = $"{source}; {sourceId}";

        return this;
    }

    public OrgClientRequestHeadersScope WithEditMode(bool enableEditMode)
    {
        Headers["x-pro-edit-mode"] = $"{enableEditMode}";
        return this;
    }

    public OrgClientRequestHeadersScope WithHeader(string key, string value)
    {
        Headers[key] = value;
        return this;
    }

    public void Dispose()
    {
        Current.Value = null;
    }
}