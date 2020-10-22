using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {

        internal class Initialize : TrackableRequest
        {
            public Initialize(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }


            public class Handler : AsyncRequestHandler<Initialize>
            {
                private readonly ResourcesDbContext resourcesDb;
                private readonly IProjectOrgResolver orgResolver;
                private readonly IMediator mediator;

                public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver, IMediator mediator)
                {
                    this.resourcesDb = resourcesDb;
                    this.orgResolver = orgResolver;
                    this.mediator = mediator;
                }

                private DbContractorRequest dbItem = null!;
                private ApiProjectContractV2 contract = null!;

                private async Task ValidateAsync(Initialize request)
                {
                    dbItem = await resourcesDb.ContractorRequests
                        .Include(r => r.Project)
                        .Include(r => r.Contract)
                        .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                    var resolvedContract = await orgResolver.ResolveContractAsync(dbItem.Project.OrgProjectId, dbItem.Contract.OrgContractId);

                    if (resolvedContract == null)
                        throw new InvalidOperationException($"Cannot resolve contract for request {request.RequestId}");

                    contract = resolvedContract;
                }

                protected override async Task Handle(Initialize request, CancellationToken cancellationToken)
                {
                    await ValidateAsync(request);

                    // Check the roles for the person that is executing the request.
                    if (IsExternalRep(request.Editor))
                    {
                        // Set state to submitted
                        await mediator.Send(new SetState(request.RequestId, DbRequestState.SubmittedToCompany));
                    }
                    else
                    {
                        await mediator.Publish(new RequestCreated(request.RequestId));
                    }
                }

                private bool IsExternalRep(CommandEditor editor)
                {
                    if (editor.Person != null)
                    {
                        // Not checking for expired positions here.
                        if (contract.ExternalContractRep != null && contract.ExternalContractRep.Instances.Any(i => i.AssignedPerson?.AzureUniqueId == editor.AzureUniqueId))
                            return true;

                        if (contract.ExternalCompanyRep != null && contract.ExternalCompanyRep.Instances.Any(i => i.AssignedPerson?.AzureUniqueId == editor.AzureUniqueId))
                            return true;
                    }

                    return false;
                }

            }
        }
    }

    
}
