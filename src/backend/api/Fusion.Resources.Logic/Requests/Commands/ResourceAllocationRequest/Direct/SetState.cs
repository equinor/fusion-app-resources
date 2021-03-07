using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Direct
        {
            //public class SetState : TrackableRequest
            //{
            //    public SetState(Guid requestId, DbResourceAllocationRequestState state)
            //    {
            //        RequestId = requestId;
            //        State = state;
            //    }
            //    public Guid RequestId { get; set; }
            //    public DbResourceAllocationRequestState State { get; set; }

            //    public class Handler : AsyncRequestHandler<SetState>
            //    {
            //        private readonly ResourcesDbContext resourcesDb;
            //        private readonly IMediator mediator;
            //        private INotification? notifyOnSave = null;

            //        public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
            //        {
            //            this.resourcesDb = resourcesDb;
            //            this.mediator = mediator;
            //        }

            //        private DbResourceAllocationRequest dbItem = null!;
            //        private InternalRequestDirectWorkflowV1 workflow = null!;

            //        protected override async Task Handle(SetState request, CancellationToken cancellationToken)
            //        {
            //            dbItem = await resourcesDb.ResourceAllocationRequests
            //                .Include(r => r.Project)
            //                .FirstOrDefaultAsync(r => r.Id == request.RequestId);

            //            if (dbItem == null)
            //                throw new RequestNotFoundError(request.RequestId);

            //            var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbItem.Id);
            //            workflow = new InternalRequestDirectWorkflowV1(dbWorkflow);


            //            switch (dbItem.State)
            //            {
            //                case DbResourceAllocationRequestState.Created:
            //                    await mediator.Send(QueueRequestProvisioning.InternalPersonnelRequest(request.RequestId, dbItem.Project.OrgProjectId));
            //                    break;

            //                default:
            //                    throw new IllegalStateChangeError(dbItem.State, request.State);
            //            }

            //            dbItem.State = request.State;
            //            dbItem.LastActivity = DateTime.UtcNow;

            //            // Update the encapsulated db entity with the new workflow state.
            //            workflow.SaveChanges();

            //            await resourcesDb.SaveChangesAsync();

            //            if (notifyOnSave != null)
            //                await mediator.Publish(notifyOnSave);
            //        }
            //    }
            //}
        
        }
    }
}