using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {
        /// <summary>
        /// Check the current user access on the specified request.
        /// </summary>
        public class CheckUserAccess : TrackableRequest<bool>
        {
            public CheckUserAccess(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }


            public class Handler : IRequestHandler<CheckUserAccess, bool>
            {
                private readonly ResourcesDbContext resourcesDb;
                private readonly IProjectOrgResolver orgResolver;

                public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver)
                {
                    this.resourcesDb = resourcesDb;
                    this.orgResolver = orgResolver;
                }

                public async Task<bool> Handle(CheckUserAccess request, CancellationToken cancellationToken)
                {
                    var dbRequest = await resourcesDb.ContractorRequests
                        .Where(r => r.Id == request.RequestId)
                        .Select(r => new { r.State, r.Project.OrgProjectId, r.Contract.OrgContractId })
                        .FirstOrDefaultAsync();

                    if (dbRequest == null)
                        return false;


                    var contract = await orgResolver.ResolveContractAsync(dbRequest.OrgProjectId, dbRequest.OrgContractId);

                    if (contract == null)
                        return false;

                    if (request.Editor.AzureUniqueId == null)
                        return false;

                    var userAzureId = request.Editor.AzureUniqueId.Value;

                    switch (dbRequest.State)
                    {
                        case DbRequestState.Created:
                            return contract.ExternalCompanyRep.HasActiveAssignment(userAzureId) || contract.ExternalContractRep.HasActiveAssignment(userAzureId);

                        case DbRequestState.SubmittedToCompany:
                            return contract.CompanyRep.HasActiveAssignment(userAzureId) || contract.ContractRep.HasActiveAssignment(userAzureId);

                        default:
                            return false;
                    }

                    
                }
            }
        }
    }

    
}
