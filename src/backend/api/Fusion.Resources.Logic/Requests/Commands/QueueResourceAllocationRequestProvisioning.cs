using Fusion.Resources.Integration.Models.Queue;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    internal class QueueResourceAllocationRequestProvisioning : IRequest
    {

        private QueueResourceAllocationRequestProvisioning(RequestType type, Guid requestId, Guid orgProjectId)
        {
            Type = type;
            RequestId = requestId;
            OrgProjectId = orgProjectId;
        }

        private RequestType Type { get; }
        public Guid RequestId { get; }
        public Guid OrgProjectId { get; }

        private enum RequestType { Employee }

        public static QueueResourceAllocationRequestProvisioning PersonnelRequest(Guid requestId, Guid orgProjectId) =>
            new QueueResourceAllocationRequestProvisioning(RequestType.Employee, requestId, orgProjectId);

        public class Handler : AsyncRequestHandler<QueueResourceAllocationRequestProvisioning>
        {
            private readonly IQueueSender queueSender;

            public Handler(IQueueSender queueSender)
            {
                this.queueSender = queueSender;
            }

            protected override async Task Handle(QueueResourceAllocationRequestProvisioning request, CancellationToken cancellationToken)
            {
                await queueSender.SendMessageAsync(QueuePath.ProvisionPosition, new ProvisionPositionRequestMessageV1
                {
                    RequestId = request.RequestId,
                    ProjectOrgId = request.OrgProjectId,
                    Type = request.Type switch
                    {
                        RequestType.Employee => ProvisionPositionRequestMessageV1.RequestTypeV1.Employee,
                        _ => throw new NotSupportedException($"Provision of request type {request.Type} is not supported")
                    }
                });
            }
        }
    }
}
