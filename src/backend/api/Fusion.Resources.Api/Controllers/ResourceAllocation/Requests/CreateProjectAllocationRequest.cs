using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateProjectAllocationRequest
    {
        public Guid? Id { get; set; }
        public string? Discipline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiResourceAllocationRequest.ApiAllocationRequestType Type { get; set; }
        public Guid OrgPositionId { get; set; }
        public ApiPositionInstance OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }
        public IEnumerable<ApiProposedChange> ProposedChanges { get; set; } = new List<ApiProposedChange>();
        public Guid ProposedPersonId { get; set; }
        public bool IsDraft { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateProjectAllocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Id).NotEmptyIfProvided();
                RuleFor(x => x.Discipline).NotContainScriptTag().MaximumLength(5000);
                RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

               /* RuleFor(x => x.OrgPositionInstance.AppliesTo)
                    .GreaterThan(x => x.OrgPositionInstance.AppliesFrom)
                    .WithMessage("From date cannot be after end date");*/

            }
        }

        #endregion
    }
}
