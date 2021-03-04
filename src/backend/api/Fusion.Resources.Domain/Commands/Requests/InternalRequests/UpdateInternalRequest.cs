using Fusion.Resources.Database;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public MonitorableProperty<bool> IsDraft { get; set; } = new();



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

                modified |= request.AssignedDepartment.IfSet(dep => dbRequest.AssignedDepartment = dep);
                modified |= request.AdditionalNote.IfSet(note => dbRequest.AdditionalNote = note);
                modified |= request.ProposedChanges.IfSet(changes => dbRequest.ProposedChanges = changes.SerializeToString());
                modified |= request.IsDraft.IfSet(d => dbRequest.IsDraft = d);
                modified |= await request.ProposedPersonAzureUniqueId.IfSetAsync(async personId =>
                {
                    if (personId is not null)
                        dbRequest.ProposedPerson = await profileService.EnsurePersonAsync(new PersonId(personId.Value));
                    else
                        dbRequest.ProposedPerson = null;
                });


                if (modified)
                {
                    dbRequest.Updated = DateTimeOffset.UtcNow;
                    dbRequest.UpdatedBy = request.Editor.Person;
                    dbRequest.LastActivity = dbRequest.Updated.Value;

                    await db.SaveChangesAsync();
                }


                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                return requestItem!;
            }
        }
    }
}
