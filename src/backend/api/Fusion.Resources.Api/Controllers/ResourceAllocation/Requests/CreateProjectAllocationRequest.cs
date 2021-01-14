using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;
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
        public ApiProjectReference Project { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiResourceAllocationRequestOrgPositionInstance? OrgPositionInstance { get; set; }
        public string? AdditionalNote { get; set; }
        public IEnumerable<ApiProposedChange>? ProposedChanges { get; set; }
        public ApiPerson? ProposedPerson { get; set; }


        #region Validator

        public class Validator : AbstractValidator<CreateProjectAllocationRequest>
        {
            public Validator(ICompanyResolver companyResolver, IProjectOrgResolver orgResolver)
            {
                RuleFor(x => x.Id).NotEmptyIfProvided().WithName("id");
                RuleFor(x => x.Discipline).NotContainScriptTag();
                RuleFor(x => x.Discipline).MaximumLength(5000);

               /* RuleFor(x => x.ToDate).GreaterThan(x => x.FromDate)
                    .WithMessage("From date cannot be after end date")
                    .When(x => x.FromDate.HasValue && x.ToDate.HasValue);*/

            }
        }

        #endregion
    }
}
