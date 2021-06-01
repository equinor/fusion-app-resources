using Fusion.AspNetCore.FluentAuthorization;
using System;
using System.Runtime.Serialization;

namespace Fusion.Resources.Logic.Requests
{
    [Serializable]
    public class UnauthorizedWorkflowException : Exception
    {
        public UnauthorizedWorkflowException()
        {
        }

        public UnauthorizedWorkflowException(string? message) : base(message)
        {
        }

        public UnauthorizedWorkflowException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UnauthorizedWorkflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public IReportableAuthorizationRequirement[] Requirements { get; set; } = Array.Empty<IReportableAuthorizationRequirement>();
    }
}