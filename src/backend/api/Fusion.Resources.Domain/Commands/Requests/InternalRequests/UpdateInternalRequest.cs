using Fusion.Resources.Database;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications;

namespace Fusion.Resources.Domain.Commands
{

    public class UpdateInternalRequest : TrackableRequest<QueryResourceAllocationRequest>
    {
        public UpdateInternalRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public MonitorableProperty<string?> AssignedDepartment { get; set; } = new();
        public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; set; } = new();
        public MonitorableProperty<string?> AdditionalNote { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; set; } = new();

        public MonitorableProperty<DateTime?> ProposalChangeFrom { get; set; } = new();
        public MonitorableProperty<DateTime?> ProposalChangeTo { get; set; } = new();
        public MonitorableProperty<ProposalChangeScope> ProposalScope { get; set; } = new();

        // Placeholder, not used currently
        public MonitorableProperty<string?> ProposalChangeType { get; set; } = new();



        public class Handler : IRequestHandler<UpdateInternalRequest, QueryResourceAllocationRequest>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IProfileService profileService, IMediator mediator)
            {
                this.db = db;
                this.profileService = profileService;
                this.mediator = mediator;
            }

            public async Task<QueryResourceAllocationRequest> Handle(UpdateInternalRequest request, CancellationToken cancellationToken)
            {
                var dbRequest = await db.ResourceAllocationRequests.FirstAsync(r => r.Id == request.RequestId);


                bool modified = false;
                bool newProposedPerson = false;

                modified |= request.AssignedDepartment.IfSet(dep => dbRequest.AssignedDepartment = dep);
                modified |= request.AdditionalNote.IfSet(note => dbRequest.AdditionalNote = note);
                modified |= request.ProposedChanges.IfSet(changes => dbRequest.ProposedChanges = changes.SerializeToStringOrDefault());
                modified |= await request.ProposedPersonAzureUniqueId.IfSetAsync(async personId =>
                {
                    if (personId is not null)
                    {
                        var resolvedPerson = await profileService.EnsurePersonAsync(new PersonId(personId.Value));
                        dbRequest.ProposedPerson.AzureUniqueId = resolvedPerson?.AzureUniqueId;
                        dbRequest.ProposedPerson.Mail = resolvedPerson?.Mail;
                        dbRequest.ProposedPerson.HasBeenProposed = true;
                        dbRequest.ProposedPerson.ProposedAt = DateTimeOffset.Now;
                        newProposedPerson = true;
                    }
                    else
                        dbRequest.ProposedPerson.Clear();
                });
                modified |= request.ProposalChangeFrom.IfSet(dt => dbRequest.ProposalParameters.ChangeFrom = dt);
                modified |= request.ProposalChangeTo.IfSet(dt => dbRequest.ProposalParameters.ChangeTo = dt);
                modified |= request.ProposalChangeType.IfSet(dt => dbRequest.ProposalParameters.ChangeType = dt);
                modified |= request.ProposalScope.IfSet(dt => dbRequest.ProposalParameters.Scope = dt.MapToDatabase());

                if (modified)
                {
                    dbRequest.Updated = DateTimeOffset.UtcNow;
                    dbRequest.UpdatedBy = request.Editor.Person;
                    dbRequest.LastActivity = dbRequest.Updated.Value;

                    await db.SaveChangesAsync();

                    if (newProposedPerson)
                    {
                        await mediator.Publish(new ProposedPersonChanged(dbRequest.Id));
                    }
                }


                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                return requestItem!;
            }
        }
    }
}
