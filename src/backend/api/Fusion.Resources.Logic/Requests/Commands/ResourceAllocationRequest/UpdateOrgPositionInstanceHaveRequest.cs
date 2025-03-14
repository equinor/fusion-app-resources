using MediatR;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Logic.Commands
{
    public class UpdateOrgPositionInstanceHaveRequest : IRequest
    {
        public UpdateOrgPositionInstanceHaveRequest(Guid orgProjectId, Guid orgPositionId, Guid orgPositionInstanceId, bool haveRequest)
        {
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            OrgPositionInstanceId = orgPositionInstanceId;
            HaveRequest = haveRequest;
        }

        public Guid OrgProjectId { get; }
        public Guid OrgPositionId { get; }
        public Guid OrgPositionInstanceId { get; }
        public bool HaveRequest { get; }

        public class Handler : IRequestHandler<UpdateOrgPositionInstanceHaveRequest>
        {
            private readonly ILogger<Handler> logger;
            private readonly IOrgApiClient client;

            public Handler(IOrgApiClientFactory orgApiClientFactory, ILogger<Handler> logger)
            {
                this.logger = logger;
                this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            }

            public async Task Handle(UpdateOrgPositionInstanceHaveRequest request, CancellationToken cancellationToken)
            {
                // This command tries to update an existing position instance. If instance is not found, it may have been deleted in ORG service.
                // If unable to update instance, log error and proceed.
                try
                {
                    // If position is deleted, exception will be thrown.
                    var position = await client.GetPositionV2Async(request.OrgProjectId, request.OrgPositionId);

                    var instance = position?.Instances.FirstOrDefault(i => i.Id == request.OrgPositionInstanceId);
                    if (instance is null)
                    {
                        logger.LogWarning(
                            $"Could not locate instance {request.OrgPositionInstanceId} on the position {request.OrgPositionId} for project {request.OrgProjectId}.");
                        return;
                    }

                    var instancePatchRequest = new JObject();
                    instance.Properties = EnsureHasRequestProperty(instance.Properties, request.HaveRequest);
                    instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Properties,
                        instance.Properties);

                    var url =
                        $"/projects/{request.OrgProjectId}/positions/{request.OrgPositionId}/instances/{request.OrgPositionInstanceId}?api-version=2.0";
                    var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                    if (!updateResp.IsSuccessStatusCode)
                    {
                        logger.LogError(updateResp.Content);
                    }
                }
                catch (OrgApiError ex)
                {
                    // A removed position is a valid error, but catch the others. 
                    if (ex.HttpStatusCode != HttpStatusCode.NotFound)
                    {
                        logger.LogError(ex.Message);
                        throw;
                    }
                }
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
