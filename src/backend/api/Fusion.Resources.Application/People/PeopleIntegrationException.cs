using System;
using System.Net.Http;

namespace Fusion.Resources
{
    public class PeopleIntegrationException : Exception
    {
        public PeopleIntegrationException(string message, HttpResponseMessage response, string content) : base(message)
        {
            Response = response;
            Content = content;
        }

        public HttpResponseMessage Response { get; }
        public string Content { get; }
    }

}
