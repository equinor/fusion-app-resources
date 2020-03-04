using Fusion.ApiClients.Org;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateContractPosition : TrackableRequest<ApiClients.Org.ApiPositionV2>
    {
        public CreateContractPosition(Guid projectId, Guid contractIdentifier)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
        }

        public Guid OrgProjectId { get; set; }
        public Guid OrgContractId { get; set; }

        public Guid BasePositionId { get; set; }
        public string PositionName { get; set; }
        public string ExternalId { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public double Workload { get; set; }

        public PersonId? AssignedPerson { get; set; }

        public class Handler : IRequestHandler<CreateContractPosition, ApiClients.Org.ApiPositionV2>
        {
            private readonly IOrgApiClient orgClient;

            public Handler(IOrgApiClientFactory apiClientFactory)
            {
                this.orgClient = apiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task<ApiPositionV2> Handle(CreateContractPosition request, CancellationToken cancellationToken)
            {

                var createPositionMessage = new HttpRequestMessage(HttpMethod.Post, $"/projects/{request.OrgProjectId}/contracts/{request.OrgContractId}/positions");
                createPositionMessage.Content = new StringContent(JsonConvert.SerializeObject(new ApiPositionV2
                {
                    BasePosition = new ApiBasePositionV2 { Id = request.BasePositionId },
                    Name = request.PositionName,
                    ExternalId = request.ExternalId,
                    Instances = new List<ApiPositionInstanceV2>
                    {
                        new ApiPositionInstanceV2
                        {
                            AppliesFrom = request.AppliesFrom,
                            AppliesTo = request.AppliesTo,
                            Workload = request.Workload,
                            AssignedPerson =  request.AssignedPerson
                        }
                    }
                }), Encoding.UTF8, "application/json");


                var resp = await orgClient.SendAsync(createPositionMessage);
                var responseContent = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode)
                {
                    var newPosition = JsonConvert.DeserializeObject<ApiPositionV2>(responseContent);
                    return newPosition;
                }

                throw new OrgApiError(resp, responseContent);
            }
        }

    }
}
