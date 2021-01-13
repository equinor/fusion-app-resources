using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class TerminateProjectAllocationRequest
    {
       
        #region Validator

        public class Validator : AbstractValidator<TerminateProjectAllocationRequest>
        {
            public Validator(ICompanyResolver companyResolver, IProjectOrgResolver orgResolver)
            {
                //RuleFor(x => x.Id).NotEmptyIfProvided().WithName("id");
            }
        }

        #endregion
    }
}
