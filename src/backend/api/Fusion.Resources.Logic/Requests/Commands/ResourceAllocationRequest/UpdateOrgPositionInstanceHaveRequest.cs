using Fusion.ApiClients.Org;
using MediatR;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Queries;

namespace Fusion.Resources.Logic.Commands
{
    public class UpdateOrgPositionInstanceHaveRequest : IRequest
    {
        public UpdateOrgPositionInstanceHaveRequest(Guid requestId, bool haveRequest)
        {
            RequestId = requestId;
            HaveRequest = haveRequest;
        }

        public Guid RequestId { get; }
        public bool HaveRequest { get; }

        public class Handler : AsyncRequestHandler<UpdateOrgPositionInstanceHaveRequest>
        {
            private readonly IMediator mediator;
            private readonly IOrgApiClient client;

            public Handler(IOrgApiClientFactory orgApiClientFactory, IMediator mediator)
            {
                this.mediator = mediator;
                this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            }

            protected override async Task Handle(UpdateOrgPositionInstanceHaveRequest request, CancellationToken cancellationToken)
            {

                var item = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                if (item is null)
                    throw new InvalidOperationException($"Request with id{request.RequestId} not found");

                var position = await client.GetPositionV2Async(item.Project.OrgProjectId, item.OrgPosition!.Id);

                var instance = position?.Instances.FirstOrDefault(i => i.Id == item.OrgPositionInstance!.Id);
                if (instance is null)
                    throw new InvalidOperationException($"Could not locate instance {item.OrgPositionInstance!.Id} on the position {item.OrgPosition!.Id} for project {item.Project.OrgProjectId}.");

                var instancePatchRequest = new JObject();
                instance.Properties = EnsureHasRequestProperty(instance.Properties, request.HaveRequest);
                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Properties, instance.Properties);

                var url = $"/projects/{item.Project.OrgProjectId}/positions/{item.OrgPosition!.Id}/instances/{item.OrgPositionInstance!.Id}?api-version=2.0";
                var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                if (!updateResp.IsSuccessStatusCode)
                    throw new OrgApiError(updateResp.Response, updateResp.Content);
            }

            private static ApiPropertiesCollectionV2 EnsureHasRequestProperty(ApiPropertiesCollectionV2? properties, bool value)
            {
                properties ??= new ApiPropertiesCollectionV2();
                if (properties.ContainsKey("hasRequest", true))
                {
                    properties["hasRequest"] = value;
                }
                else
                {
                    properties.Add("hasRequest", value);
                }
                return properties;
            }
        }
    }
}
