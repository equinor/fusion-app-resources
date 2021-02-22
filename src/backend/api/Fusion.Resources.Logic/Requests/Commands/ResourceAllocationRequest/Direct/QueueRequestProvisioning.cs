using Fusion.Resources.Integration.Models.Queue;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Direct
        {
            internal class QueueRequestProvisioning : IRequest
            {
                private QueueRequestProvisioning(RequestType type, Guid requestId, Guid orgProjectId)
                {
                    Type = type;
                    RequestId = requestId;
                    OrgProjectId = orgProjectId;
                }

                private RequestType Type { get; }
                public Guid RequestId { get; }
                public Guid OrgProjectId { get; }

                private enum RequestType
                {
                    Employee
                }

                public static QueueRequestProvisioning EmployeeRequest(Guid requestId, Guid orgProjectId) => new QueueRequestProvisioning(RequestType.Employee, requestId, orgProjectId);

                public class Handler : AsyncRequestHandler<QueueRequestProvisioning>
                {
                    private readonly IQueueSender queueSender;

                    public Handler(IQueueSender queueSender)
                    {
                        this.queueSender = queueSender;
                    }

                    protected override async Task Handle(QueueRequestProvisioning request,
                        CancellationToken cancellationToken)
                    {
                        await queueSender.SendMessageAsync(QueuePath.ProvisionPosition, new ProvisionInternalRequestPositionMessageV1
                        {
                            RequestId = request.RequestId,
                            ProjectOrgId = request.OrgProjectId,
                            Type = request.Type switch
                            {
                                RequestType.Employee => ProvisionInternalRequestPositionMessageV1.RequestTypeV1.Employee,
                                _ => throw new NotSupportedException(
                                    $"Provision of request type {request.Type} is not supported")
                            }
                        });
                    }
                }
            }
        }
    }
}