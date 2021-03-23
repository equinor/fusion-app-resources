using FluentValidation;
using FluentValidation.Results;
using Fusion.Resources.Logic.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Logic
{
    public class InvalidWorkflowError : ValidationException
    {
        public string? WorkflowTypeName { get; set; }

        public InvalidWorkflowError(string message, IEnumerable<ValidationFailure> errors) : base(message, errors)
        {
        }

        public static InvalidWorkflowError ValidationError<TWorkflow>(string message, Action<ErrorBuilder>? setup)
            where TWorkflow : WorkflowDefinition
        {
            var builder = new ErrorBuilder();
            setup?.Invoke(builder);

            return new InvalidWorkflowError(message, builder.Failures)
            {
                WorkflowTypeName = typeof(TWorkflow).Name
            };
        }


        public class ErrorBuilder
        {
            List<ValidationFailure> failures = new List<ValidationFailure>();

            public ErrorBuilder AddFailure(string field, string message)
            {
                failures.Add(new ValidationFailure(field, message));
                return this;
            }

            public IEnumerable<ValidationFailure> Failures => Array.Empty<ValidationFailure>();

        }
    }
}
