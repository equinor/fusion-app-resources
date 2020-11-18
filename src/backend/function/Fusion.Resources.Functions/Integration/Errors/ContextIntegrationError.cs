using System;
using System.Net;

namespace Fusion.Resources.Functions.Integration
{
    public class ContextIntegrationError : Exception
    {
        public ContextIntegrationError(string url, HttpStatusCode httpStatusCode) : base($"Received status [{httpStatusCode}] when accessing url '{url}' in context service")
        {
        }
    }
}
