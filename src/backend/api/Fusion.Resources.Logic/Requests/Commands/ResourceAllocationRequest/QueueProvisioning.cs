using Fusion.Resources.Database;
using Fusion.Resources.Integration.Models.Queue;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class QueueProvisioning : IRequest
        {
            public QueueProvisioning(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }

            public class Handler : IRequestHandler<QueueProvisioning>
            {
                private readonly int FixedDelayInSecondsBeforeProvisioning = 5;
                private readonly ResourcesDbContext dbContext;
                private readonly IQueueSender queueSender;

                public Handler(ResourcesDbContext dbContext, IQueueSender queueSender)
                {
                    this.dbContext = dbContext;
                    this.queueSender = queueSender;
                }

                public async Task Handle(QueueProvisioning request, CancellationToken cancellationToken)
                {
                    var dbRequest = await dbContext.ResourceAllocationRequests.FindAsync(request.RequestId);

                    //Reason for delay, is that we have to ensure logic surrounding workflow/request is finished before service bus queue starts provisioning.
                    if (dbRequest != null)
                        await queueSender.SendMessageDelayedAsync(QueuePath.ProvisionPosition,
                            new ProvisionPositionMessageV1
                            {
                                RequestId = request.RequestId,
                                ProjectOrgId = dbRequest.Project.OrgProjectId,
                                Type = ProvisionPositionMessageV1.RequestTypeV1.InternalPersonnel
                            }, FixedDelayInSecondsBeforeProvisioning);
                }
            }
        }
    }
}