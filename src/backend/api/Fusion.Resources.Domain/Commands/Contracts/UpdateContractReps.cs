using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update company rep positions.
    /// This will update the company rep & contract responsible.
    /// </summary>
    public class UpdateContractReps : IRequest
    {
        public UpdateContractReps(Guid orgProjectId, Guid orgContractId)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        public Guid OrgProjectId { get; set; }
        public Guid OrgContractId { get; set; }

        public MonitorableProperty<Guid?> CompanyRepPositionId { get; set; } = new MonitorableProperty<Guid?>();
        public MonitorableProperty<Guid?> ContractResponsiblePositionId { get; set; } = new MonitorableProperty<Guid?>();

        public class Handler : AsyncRequestHandler<UpdateContractReps>
        {
            private readonly IOrgApiClient orgClient;
            private readonly IMediator mediator;

            public Handler(IOrgApiClientFactory orgApiClientFactory, IMediator mediator)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.mediator = mediator;
            }

            protected override async Task Handle(UpdateContractReps request, CancellationToken cancellationToken)
            {
                if (!request.CompanyRepPositionId.HasBeenSet && !request.ContractResponsiblePositionId.HasBeenSet)
                    throw new ArgumentException("Either company rep or contract responsible must be set.");

                ApiProjectContractV2 contract;

                try
                {
                    contract = await orgClient.GetContractV2Async(request.OrgProjectId, request.OrgContractId);
                }
                catch (OrgApiError ex)
                {
                    if (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new ArgumentException($"Could not locate contract by id '{request.OrgContractId}'");

                    throw new InvalidOperationException($"Error trying to get error from org service. Received {ex.Response.StatusCode}, {ex.Error?.Message} ({ex.Error?.ErrorCode})", ex);
                }

                if (request.CompanyRepPositionId.HasBeenSet)
                {
                    contract.CompanyRep = request.CompanyRepPositionId.Value.HasValue ? new ApiPositionV2 { Id = request.CompanyRepPositionId.Value.Value } : null;

                    if (contract.CompanyRep != null)
                        await mediator.Publish(new Notifications.CompanyRepUpdated(contract.CompanyRep.Id));
                }

                if (request.ContractResponsiblePositionId.HasBeenSet)
                {
                    contract.ContractRep = request.ContractResponsiblePositionId.Value.HasValue ? new ApiPositionV2 { Id = request.ContractResponsiblePositionId.Value.Value } : null;

                    if (contract.ContractRep != null)
                        await mediator.Publish(new Notifications.ContractRepUpdated(contract.ContractRep.Id));
                }
            }
        }
    }
}
