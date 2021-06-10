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

        public object ToErrorObject()
        {
            return new
            {
                error = new
                {
                    code = "Forbidden",
                    message = "You do not meet any of the requirements to access the underlying data.",
                    accessRequirements = Requirements
                }
            };
        }
    }
}