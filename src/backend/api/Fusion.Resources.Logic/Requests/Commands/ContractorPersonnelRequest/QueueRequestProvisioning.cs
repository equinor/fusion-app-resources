using Fusion.Resources.Integration.Models.Queue;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    internal class QueueRequestProvisioning : IRequest
    {
        private QueueRequestProvisioning(RequestType type, Guid requestId, Guid orgProjectId,
            Guid orgContractId)
        {
            Type = type;
            RequestId = requestId;
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        private RequestType Type { get; }
        public Guid RequestId { get; }
        public Guid OrgProjectId { get; }
        public Guid OrgContractId { get; }

        private enum RequestType
        {
            ContractorPersonnel
        }

        public static QueueRequestProvisioning ContractorPersonnelRequest(Guid requestId, Guid orgProjectId,
            Guid orgContractId) =>
            new QueueRequestProvisioning(RequestType.ContractorPersonnel, requestId, orgProjectId,
                orgContractId);

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
                await queueSender.SendMessageAsync(QueuePath.ProvisionPosition, new ProvisionPositionMessageV1
                {
                    RequestId = request.RequestId,
                    ProjectOrgId = request.OrgProjectId,
                    ContractOrgId = request.OrgContractId,
                    Type = request.Type switch
                    {
                        RequestType.ContractorPersonnel => ProvisionPositionMessageV1.RequestTypeV1
                            .ContractorPersonnel,
                        _ => throw new NotSupportedException(
                            $"Provision of request type {request.Type} is not supported")
                    }
                });
            }
        }
    }
}
