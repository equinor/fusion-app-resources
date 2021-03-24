using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class ResourceOwner
        {
            internal class ProvisionResourceOwnerRequest : IRequest
            {
                public ProvisionResourceOwnerRequest(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }


                public class Handler : AsyncRequestHandler<ProvisionResourceOwnerRequest>
                {
                    private readonly IOrgApiClient client;
                    private readonly ResourcesDbContext resourcesDb;

                    public Handler(ResourcesDbContext resourcesDb, IOrgApiClientFactory orgApiClientFactory)
                    {
                        this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                        this.resourcesDb = resourcesDb;
                    }

                    protected override async Task Handle(ProvisionResourceOwnerRequest request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                        await ProvisionAsync(dbRequest);
                    }

                    private async Task ProvisionAsync(DbResourceAllocationRequest dbRequest)
                    {
                        // Need to get the raw position, in case model has changed.. When putting position, fields not 
                        var rawPosition = await client.GetAsync<JObject>($"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0");

                        var position = rawPosition.Value.ToObject<ApiPositionV2>();
                        var positionInstance = position!.Instances.First(i => i.Id == dbRequest.OrgPositionInstance.Id);

                        var isFuture = positionInstance.AppliesFrom >= DateTime.UtcNow.Date;

                        var effectiveChangeFrom = dbRequest.ProposalParameters.ChangeFrom?.Date ?? positionInstance.AppliesFrom.Date;
                        var effectiveChangeTo = dbRequest.ProposalParameters.ChangeTo?.Date ?? positionInstance.AppliesTo.Date;
                        if (effectiveChangeFrom < positionInstance.AppliesFrom) effectiveChangeFrom = positionInstance.AppliesFrom.Date;
                        if (effectiveChangeTo > positionInstance.AppliesTo) effectiveChangeTo = positionInstance.AppliesTo.Date;

                        var isSameDates = effectiveChangeFrom == positionInstance.AppliesFrom && effectiveChangeTo == positionInstance.AppliesTo;

                        if (isFuture && isSameDates)
                            await UpdateFutureSplitAsync(dbRequest, rawPosition.Value);
                        else
                        {

                            var equalStart = effectiveChangeFrom.Date == positionInstance.AppliesFrom.Date;
                            var equalEnd = effectiveChangeTo.Date == positionInstance.AppliesTo.Date;


                            if (equalStart)
                                await UpdateStartAsync(dbRequest, rawPosition.Value, effectiveChangeTo);
                            else if (equalEnd)
                                await UpdateEndAsync(dbRequest, rawPosition.Value, effectiveChangeFrom);
                            else
                                await UpdateCenterAsync(dbRequest, rawPosition.Value, effectiveChangeFrom, effectiveChangeTo);
                        }
                    }

                    private async Task UpdateStartAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeTo)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var newInstanceStartDate = changeTo.Date.AddDays(1);
                        var existingInstanceEndDate = changeTo;


                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>().First(i => i.GetPropertyValue<ApiPositionInstanceV2, Guid>(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newInstance = new JObject(instanceToUpdate);

                        // Start the new instance where the change should apply to
                        newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, newInstanceStartDate);

                        // Stop the current instance at the new date
                        instanceToUpdate.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, existingInstanceEndDate);

                        newInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        ApplyProposedChanges(dbRequest, instanceToUpdate);

                        // Add the new instance with the changes to the position
                        instances.Add(newInstance);

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        var resp = await client.PutAsync(url, rawPosition);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }

                    private async Task UpdateEndAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeFrom)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;


                        var originalInstanceEndDate = changeFrom.Date;
                        var newInstanceStartDate = originalInstanceEndDate.AddDays(1);

                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>().First(i => i.GetPropertyValue<ApiPositionInstanceV2, Guid>(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newInstance = new JObject(instanceToUpdate);

                        // Stop the current instance at the applicable date
                        instanceToUpdate.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, originalInstanceEndDate);
                        newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, newInstanceStartDate);

                        newInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        ApplyProposedChanges(dbRequest, newInstance);

                        // Add the new instance with the changes to the position
                        instances.Add(newInstance);

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        var resp = await client.PutAsync(url, rawPosition);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }

                    private async Task UpdateCenterAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeFrom, DateTime changeTo)
                    {
                        /*
                         * Basically we are splitting the current instance in to three parts.
                         * 
                         * <---> <new> <--->
                         * 
                         * So we must update the targeted instance to stop on the new change from (- 1 day)
                         * Create two new splits, 1 with the changeFrom and changeTo date; and 1 with changeTo + 1 day and the original end date.
                         * 
                         * */



                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;


                        var existingInstanceEndDate = changeFrom.AddDays(-1);
                        var trailingInstanceStartDate = changeTo.AddDays(1);


                        var originalInstanceEndDate = changeFrom.Date;
                        var newInstanceStartDate = originalInstanceEndDate.AddDays(1);



                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>().First(i => i.GetPropertyValue<ApiPositionInstanceV2, Guid>(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newCenterInstance = new JObject(instanceToUpdate);
                        var newTrailingInstance = new JObject(instanceToUpdate);

                        // Stop the current instance at the applicable date
                        instanceToUpdate.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, existingInstanceEndDate);

                        newCenterInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, changeFrom);
                        newCenterInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, changeTo);

                        newTrailingInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, trailingInstanceStartDate);

                        // clean out ids
                        newCenterInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));
                        newTrailingInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        // Apply changes to the center split we are creating
                        ApplyProposedChanges(dbRequest, newCenterInstance);

                        // Add the new instance with the changes to the position
                        instances.Add(newCenterInstance);
                        instances.Add(newTrailingInstance);

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        var resp = await client.PutAsync(url, rawPosition);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }

                    private void ApplyProposedChanges(DbResourceAllocationRequest dbRequest, JObject instance)
                    {
                        var proposedChanges = new JObject();

                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                        if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                            instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);
                    }


                    private async Task UpdateFutureSplitAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var instancePatchRequest = new JObject();

                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        if (proposedChanges.TryGetValue("obs", StringComparison.InvariantCultureIgnoreCase, out var obs))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Obs, obs);

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                        if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, appliesFrom);

                        if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, appliesTo);

                        if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);


                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);


                        // Update next instance if applies to date has changed
                        if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out appliesTo))
                        {
                            var pos = rawPosition.ToObject<ApiPositionV2>();
                            pos.Instances
                                .Where(i => i.Type == ApiInstanceType.Normal && i.AppliesFrom > appliesTo.ToObject<DateTime?>())
                                .OrderBy(i => i.AppliesFrom)
                                .FirstOrDefault();


                        }
                    }


                }
            }
        }
    }
}
