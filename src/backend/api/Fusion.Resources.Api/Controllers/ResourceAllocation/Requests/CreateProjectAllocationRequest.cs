using System;
using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateProjectAllocationRequest
    {
       
        #region Validator

        public class Validator : AbstractValidator<CreateProjectAllocationRequest>
        {
            public Validator(ICompanyResolver companyResolver, IProjectOrgResolver orgResolver)
            {
                //RuleFor(x => x.Id).NotEmptyIfProvided().WithName("id");
            }
        }

        #endregion

        public Guid RequestNumber { get; set; }
    }
}
