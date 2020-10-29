using Fusion.ApiClients.Org;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Update the external reps.
    /// </summary>
    public class UpdateContractExternalReps : IRequest
    {
        public UpdateContractExternalReps(Guid orgProjectId, Guid orgContractId)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        public Guid OrgProjectId { get; set; }
        public Guid OrgContractId { get; set; }

        public MonitorableProperty<Guid?> CompanyRepPositionId { get; set; } = new MonitorableProperty<Guid?>();
        public MonitorableProperty<Guid?> ContractResponsiblePositionId { get; set; } = new MonitorableProperty<Guid?>();

        public class Handler : AsyncRequestHandler<UpdateContractExternalReps>
        {
            private readonly IOrgApiClient orgClient;
            private readonly IMediator mediator;

            public Handler(IOrgApiClientFactory orgApiClientFactory, IMediator mediator)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.mediator = mediator;
            }

            protected override async Task Handle(UpdateContractExternalReps request, CancellationToken cancellationToken)
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

                bool notifyCompanyRep = false;
                bool notifyContractRep = false;

                if (request.CompanyRepPositionId.HasBeenSet)
                {
                    if (request.CompanyRepPositionId.Value.HasValue && contract.ExternalCompanyRep?.Id != request.CompanyRepPositionId.Value.Value)
                        notifyCompanyRep = true;

                    contract.ExternalCompanyRep = request.CompanyRepPositionId.Value.HasValue ? new ApiPositionV2 { Id = request.CompanyRepPositionId.Value.Value } : null;
                }

                if (request.ContractResponsiblePositionId.HasBeenSet)
                {
                    if (request.ContractResponsiblePositionId.Value.HasValue && contract.ExternalContractRep?.Id != request.ContractResponsiblePositionId.Value.Value)
                        notifyContractRep = true;

                    contract.ExternalContractRep = request.ContractResponsiblePositionId.Value.HasValue ? new ApiPositionV2 { Id = request.ContractResponsiblePositionId.Value.Value } : null;
                }

                await orgClient.UpdateContractV2Async(request.OrgProjectId, contract);

                if (notifyCompanyRep && contract.ExternalCompanyRep != null)
                    await mediator.Publish(new Notifications.ExternalCompanyRepUpdated(contract.ExternalCompanyRep.Id));

                if (notifyContractRep && contract.ExternalContractRep != null)
                    await mediator.Publish(new Notifications.ExternalContractRepUpdated(contract.ExternalContractRep.Id));
            }
        }
    }
}
