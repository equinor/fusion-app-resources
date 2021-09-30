using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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


                public class Handler : AsyncRequestHandler<ProvisionAllocationRequest>
                {
                    private IOrgApiClient client;
                    private ResourcesDbContext resourcesDb;

                    public Handler(ResourcesDbContext resourcesDb, IOrgApiClientFactory orgApiClientFactory)
                    {
                        this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                        this.resourcesDb = resourcesDb;
                    }

                    protected override async Task Handle(ProvisionAllocationRequest request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                        if (dbRequest.OrgPositionId is null)
                            throw new InvalidOperationException("Position id cannot be empty when provisioning request");

                        var position = await client.GetPositionV2Async(dbRequest.Project.OrgProjectId, dbRequest.OrgPositionId.Value);
                        var draft = await CreateProvisionDraftAsync(dbRequest);

                        await AllocateRequestInstanceAsync(dbRequest, draft, position);

                        //switch (dbRequest.ProposalParameters.Scope)
                        //{
                        //    case DbResourceAllocationRequest.DbChangeScope.Default:
                        //        await ProcessFutureInstancesAsync(dbRequest, draft, position);
                        //        break;

                        //    case DbResourceAllocationRequest.DbChangeScope.InstanceOnly:
                        //        break;
                        //}


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

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/drafts/{draft.Id}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);
                    }

                    private async Task ProcessFutureInstancesAsync(DbResourceAllocationRequest dbRequest, ApiDraftV2 draft, ApiPositionV2 position)
                    {
                        var instance = position.Instances.FirstOrDefault(i => i.Id == dbRequest.OrgPositionInstance.Id);
                        if (instance is null)
                            throw new InvalidOperationException("Could not locate instance request targets on the position.");

                        var futureTbnPositions = position.Instances.Where(i => i.AppliesFrom.Date >= instance.AppliesTo.Date && i.AssignedPerson == null);

                        /*
                         * If the split we are allocating a person to is rotation, we only want to assign to the rotation splits, not succeeding onshore parts.
                         * However if we assign to the normal onshore part, we want to also populate the first of the rotation parts.
                         * */
                        if (instance.Type == ApiInstanceType.Rotation)
                            futureTbnPositions = futureTbnPositions.Where(i => i.Type == ApiInstanceType.Rotation || i.RotationId == instance.RotationId);
                        else
                            futureTbnPositions = futureTbnPositions.Where(i => i.Type == ApiInstanceType.Normal || i.RotationId == "1");

                        var instancePatchRequest = new JObject();
                        instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        foreach (var instanceToUpdate in futureTbnPositions)
                        {
                            // Update instances
                            if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            {
                                var url = $"/projects/{dbRequest.Project.OrgProjectId}/drafts/{draft.Id}/positions/{dbRequest.OrgPositionId}/instances/{instanceToUpdate.Id}?api-version=2.0";
                                var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                                if (!updateResp.IsSuccessStatusCode)
                                    throw new OrgApiError(updateResp.Response, updateResp.Content);
                            }
                        }
                    }
                }
            }
        }
    }
}
