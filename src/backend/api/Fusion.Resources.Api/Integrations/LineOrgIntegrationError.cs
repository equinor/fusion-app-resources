using System;
using System.Runtime.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    internal class LineOrgIntegrationError : Exception
    {
        public LineOrgIntegrationError()
        {
        }

        public LineOrgIntegrationError(string? message) : base(message)
        {
        }

        public LineOrgIntegrationError(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected LineOrgIntegrationError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}