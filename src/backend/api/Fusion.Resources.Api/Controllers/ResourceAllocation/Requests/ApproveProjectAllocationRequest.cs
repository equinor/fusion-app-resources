using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApproveProjectAllocationRequest
    {
       
        #region Validator

        public class Validator : AbstractValidator<ApproveProjectAllocationRequest>
        {
            public Validator(ICompanyResolver companyResolver, IProjectOrgResolver orgResolver)
            {
                //RuleFor(x => x.Id).NotEmptyIfProvided().WithName("id");
            }
        }

        #endregion
    }
}
