using System;
using System.Net;

namespace Fusion.Resources.Functions.Integration
{
    public class IntegrationError : Exception
    {
        public IntegrationError(string url, HttpStatusCode httpStatusCode) : base($"Received status [{httpStatusCode}] when accessing url '{url}'")
        {
        }
    }
}
