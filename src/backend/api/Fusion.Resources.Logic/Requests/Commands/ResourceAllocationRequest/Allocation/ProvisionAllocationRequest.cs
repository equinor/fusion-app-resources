using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Allocation
        {
            public class ProvisionAllocationRequest : IRequest
            {
                public ProvisionAllocationRequest(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }


                public class Handler : IRequestHandler<ProvisionAllocationRequest>
                {
                    private IOrgApiClient client;
                    private IOrgHttpClient httpClient;
                    private ResourcesDbContext resourcesDb;

                    public Handler(ResourcesDbContext resourcesDb, IOrgApiClientFactory orgApiClientFactory, IOrgHttpClient httpClient)
                    {
                        this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                        this.httpClient = httpClient;
                        this.resourcesDb = resourcesDb;
                    }

                    public async Task Handle(ProvisionAllocationRequest request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                        if (dbRequest?.OrgPositionId is null)
                            throw new InvalidOperationException("Position id cannot be empty when provisioning request");

                        var position = await client.GetPositionV2Async(dbRequest.Project.OrgProjectId, dbRequest.OrgPositionId.Value);
                        var draft = await CreateProvisionDraftAsync(dbRequest);

                        await AllocateRequestInstanceAsync(dbRequest, draft, position);

                        draft = await client.PublishAndWaitAsync(draft);
                    }

                    private async Task<ApiDraftV2> CreateProvisionDraftAsync(DbResourceAllocationRequest dbRequest)
                    {
                        return await client.CreateProjectDraftAsync(dbRequest.Project.OrgProjectId, $"Allocation provisioning",
                            $"Provisioning of request [{dbRequest.Id}] to position [{dbRequest.OrgPositionId}/{dbRequest.OrgPositionInstance.Id}]");
                    }

                    private async Task AllocateRequestInstanceAsync(DbResourceAllocationRequest dbRequest, ApiDraftV2 draft, ApiPositionV2 position)
                    {
                        var instance = position.Instances.FirstOrDefault(i => i.Id == dbRequest.OrgPositionInstance.Id);
                        if (instance is null)
                            throw new InvalidOperationException("Could not locate instance request targets on the position.");


                        var instancePatchRequest = new JObject();
                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        try
                        {
                            if (dbRequest.ProposedPerson.AzureUniqueId != null)
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });


                            if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                            if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, appliesFrom);

                            if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, appliesTo);

                            if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);
                        }
                        catch (Exception ex)
                        {
                            throw new ProvisioningError("Invalid data from request", ex);
                        }

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/drafts/{draft.Id}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";

                        string json = instancePatchRequest.ToString(Formatting.None);
                        HttpContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(HttpMethod.Patch, url);
                        request.Content = content;

                        var updateResp
                            = await httpClient.SendAsync<ApiPositionInstanceV2>(request);
                            //= await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);
                    }
                }
            }
        }
    }
}
