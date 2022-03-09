using Fusion.Integration.Profile;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Check if the user should be auto approved or not.
    /// Returns null if the user does not exist.
    /// </summary>
    public class GetPersonAutoApprovalStatus : IRequest<bool?>
    {
        public GetPersonAutoApprovalStatus(Guid personAzureUniqueId)
        {
            PersonId = new PersonId(personAzureUniqueId);
        }

        public PersonId PersonId { get; }

        public class Handler : IRequestHandler<GetPersonAutoApprovalStatus, bool?>
        {
            private readonly IMediator mediator;

            public Handler(IMediator mediator)
            {
                this.mediator = mediator;
            }
            public async Task<bool?> Handle(GetPersonAutoApprovalStatus request, CancellationToken cancellationToken)
            {
                // Request requires azure unique id, so this should be ok.
                var profile = await mediator.Send(new GetPersonProfile(request.PersonId.UniqueId!.Value));

                if (profile is null)
                    return null;

                // Requirement right now is to check the account type
                if (profile.AccountType != FusionAccountType.Employee)
                    return true;


                // Check if the department has enabled for user. 
                // This disables the ability to enable approval request for contractors.
                if (!string.IsNullOrEmpty(profile.FullDepartment))
                {
                    var departmentAutoApproval = await mediator.Send(new GetDepartmentAutoApproval(profile.FullDepartment));
                    if (departmentAutoApproval?.Enabled == true)
                        return true;
                }


                // No rules affecting, return default.
                return false;
            }
        }
    }
}
