using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using MediatR;

namespace Fusion.Resources.Domain
{
    public class GetContractPosition : IRequest<ApiClients.Org.ApiPositionV2>
    {
        private GetContractPosition(Guid projectId, Guid contractId, string externalId)
        {
            Type = QueryType.ByExternalId;
            ProjectId = projectId;
            ContractId = contractId;
            ExternalId = externalId;
        }

        public QueryType Type { get; set; }
        public Guid ProjectId { get; }
        public Guid ContractId { get; }
        public string ExternalId { get; }

        public static GetContractPosition ByExternalId(Guid orgProjectId, Guid orgContractId, string id) => new GetContractPosition(orgProjectId, orgContractId, id);
        public enum QueryType { ByExternalId }


        public class Handler : IRequestHandler<GetContractPosition, ApiClients.Org.ApiPositionV2>
        {
            private readonly IOrgApiClient orgApiClient;

            public Handler(IOrgApiClientFactory orgApiClientFactory)
            {
                this.orgApiClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiClients.Org.ApiPositionV2> Handle(GetContractPosition request, CancellationToken cancellationToken)
            {
                var positions = await orgApiClient.GetAsync<List<ApiPositionV2>>($"/projects/{request.ProjectId}/contracts/{request.ContractId}/positions?$expand=project&$filter=externalId eq '{request.ExternalId}'");
                return positions.Value.FirstOrDefault();
            }
        }
    }
}
