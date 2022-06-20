using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
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
                        // Need to get the raw position, in case model has changed.. When putting position, fields not in the model will be overwritten
                        var rawPosition = await client.GetAsync<JObject>($"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0");

                        var position = rawPosition.Value.ToObject<ApiPositionV2>();
                        var positionInstance = position!.Instances.First(i => i.Id == dbRequest.OrgPositionInstance.Id);
                        var assignedPersonAzureId = positionInstance.AssignedPerson?.AzureUniqueId;

                        var isFuture = positionInstance.AppliesFrom >= DateTime.UtcNow.Date;

                        var effectiveChangeFrom = dbRequest.ProposalParameters.ChangeFrom?.Date ?? positionInstance.AppliesFrom.Date;
                        var effectiveChangeTo = dbRequest.ProposalParameters.ChangeTo?.Date ?? positionInstance.AppliesTo.Date;
                        if (effectiveChangeFrom < positionInstance.AppliesFrom) effectiveChangeFrom = positionInstance.AppliesFrom.Date;
                        if (effectiveChangeTo > positionInstance.AppliesTo) effectiveChangeTo = positionInstance.AppliesTo.Date;

                        var isSameDates = effectiveChangeFrom == positionInstance.AppliesFrom && effectiveChangeTo == positionInstance.AppliesTo;

                        var jsonPosition = new JObjectProxy<ApiPositionV2>(rawPosition.Value);

                        if (isFuture && isSameDates)
                            await UpdateFutureSplitAsync(dbRequest, jsonPosition);
                        else
                        {
                            var equalStart = effectiveChangeFrom.Date == positionInstance.AppliesFrom.Date;
                            var equalEnd = effectiveChangeTo.Date == positionInstance.AppliesTo.Date;


                            if (equalStart)
                                UpdateStart(dbRequest, jsonPosition, effectiveChangeTo);
                            else if (equalEnd)
                                UpdateEnd(dbRequest, jsonPosition, effectiveChangeFrom);
                            else
                                UpdateCenter(dbRequest, jsonPosition, effectiveChangeFrom, effectiveChangeTo);

                            // Update successor instance, either update the resource of remove.
                            UpdateSuccessorInstances(dbRequest, jsonPosition, assignedPersonAzureId);

                            await SavePositionAsync(dbRequest, jsonPosition);
                        }
                    }

                    private void UpdateSuccessorInstances(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition, Guid? assignedPersonAzureId)
                    {
                        var subType = new SubType(dbRequest.SubType);

                        // Must specify a change date
                        if (dbRequest.ProposalParameters.ChangeFrom is null)
                            return;

                        // Do not update if there is set a to date, meaning change is temporary
                        if (dbRequest.ProposalParameters.ChangeTo is not null)
                            return;

                        if (assignedPersonAzureId is null)
                            return;

                        // If the scope is just the current instance, do not touch future instances
                        if (dbRequest.ProposalParameters.Scope == DbResourceAllocationRequest.DbChangeScope.InstanceOnly)
                            return;

                        switch (subType.Value)
                        {
                            case SubType.Types.ChangeResource:
                                UpdateResourceOnInstancesAfter(rawPosition, assignedPersonAzureId.Value, dbRequest.ProposedPerson.AzureUniqueId, dbRequest.ProposalParameters.ChangeFrom!.Value);
                                break;

                            case SubType.Types.RemoveResource:
                                UpdateResourceOnInstancesAfter(rawPosition, assignedPersonAzureId.Value, null, dbRequest.ProposalParameters.ChangeFrom!.Value);
                                break;
                        }
                    }

                    private async Task SavePositionAsync(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition)
                    {

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        var resp = await client.PutAsync(url, rawPosition.JsonObject);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }

                    private void UpdateStart(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition, DateTime changeTo)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection(p => p.Instances)!;

                        var newInstanceStartDate = changeTo.Date;
                        var existingInstanceEndDate = changeTo.Date.AddDays(-1);


                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>()
                            .Select(x => new JObjectProxy<ApiPositionInstanceV2>(x))
                            .First(i => i.GetPropertyValue(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newInstance = instanceToUpdate.Clone();

                        // Start the new instance where the change should apply to
                        newInstance.SetPropertyValue(i => i.AppliesFrom, newInstanceStartDate);

                        // Stop the current instance at the new date
                        instanceToUpdate.SetPropertyValue(i => i.AppliesTo, existingInstanceEndDate);

                        newInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        ApplyProposedChanges(dbRequest, instanceToUpdate);

                        // Add the new instance with the changes to the position
                        instances.Add(newInstance.JsonObject);
                    }

                    private void UpdateEnd(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition, DateTime changeFrom)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection(p => p.Instances)!;


                        var originalInstanceEndDate = changeFrom.Date.AddDays(-1);
                        var newInstanceStartDate = changeFrom.Date;

                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>()
                            .Select(x => new JObjectProxy<ApiPositionInstanceV2>(x))
                            .First(i => i.GetPropertyValue(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newInstance = instanceToUpdate.Clone();

                        // Stop the current instance at the applicable date
                        instanceToUpdate.SetPropertyValue(i => i.AppliesTo, originalInstanceEndDate);
                        newInstance.SetPropertyValue(i => i.AppliesFrom, newInstanceStartDate);

                        newInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        ApplyProposedChanges(dbRequest, newInstance);

                        // Add the new instance with the changes to the position
                        instances.Add(newInstance.JsonObject);
                        //rawPosition.SetPropertyValue(p => p.Instances, instances);
                    }

                    private void UpdateCenter(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition, DateTime changeFrom, DateTime changeTo)
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
                        var instances = rawPosition.GetPropertyCollection(p => p.Instances)!;


                        var existingInstanceEndDate = changeFrom.Date.AddDays(-1);
                        var trailingInstanceStartDate = changeTo.Date.AddDays(1);


                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>()
                            .Select(x => new JObjectProxy<ApiPositionInstanceV2>(x))
                            .First(i => i.GetPropertyValue(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newCenterInstance = instanceToUpdate.Clone();
                        var newTrailingInstance = instanceToUpdate.Clone();

                        // Stop the current instance at the applicable date
                        instanceToUpdate.SetPropertyValue(i => i.AppliesTo, existingInstanceEndDate);

                        newCenterInstance.SetPropertyValue(i => i.AppliesFrom, changeFrom);
                        newCenterInstance.SetPropertyValue(i => i.AppliesTo, changeTo);

                        newTrailingInstance.SetPropertyValue(i => i.AppliesFrom, trailingInstanceStartDate);

                        // clean out ids
                        newCenterInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));
                        newTrailingInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));

                        // Apply changes to the center split we are creating
                        ApplyProposedChanges(dbRequest, newCenterInstance);

                        // Add the new instance with the changes to the position
                        instances.Add(newCenterInstance.JsonObject);
                        instances.Add(newTrailingInstance.JsonObject);
                        //rawPosition.SetPropertyValue(p => p.Instances, instances);
                    }

                    private void ApplyProposedChanges(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionInstanceV2> instance)
                    {
                        var subType = new SubType(dbRequest.SubType);

                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges) && dbRequest.ProposedChanges != "null")
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        switch (subType.Value)
                        {
                            case SubType.Types.Adjustment:
                                if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                                    instance.SetPropertyValue(i => i.Workload!, workload);

                                if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                                    instance.SetPropertyValue(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);
                                break;

                            case SubType.Types.ChangeResource:
                                if (dbRequest.ProposedPerson.AzureUniqueId != null)
                                    instance.SetPropertyValue(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });
                                break;

                            case SubType.Types.RemoveResource:
                                instance.SetPropertyValue(i => i.AssignedPerson, null!);
                                break;
                        }
                    }


                    private async Task UpdateFutureSplitAsync(DbResourceAllocationRequest dbRequest, JObjectProxy<ApiPositionV2> rawPosition)
                    {
                        // Update existing 
                        //var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var instancePatchRequest = new JObjectProxy<ApiPositionInstanceV2>();

                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            instancePatchRequest.SetPropertyValue(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            instancePatchRequest.SetPropertyValue(i => i.Workload!, workload);

                        /*
                        //
                        // For now we disable letting the resource owner change these properties
                        //
                        
                        if (proposedChanges.TryGetValue("obs", StringComparison.InvariantCultureIgnoreCase, out var obs))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Obs, obs);

                        if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, appliesFrom);

                        if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, appliesTo);
                        */

                        if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                            instancePatchRequest.SetPropertyValue(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);


                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest.JsonObject);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);

                    }

                    private void UpdateResourceOnInstancesAfter(JObjectProxy<ApiPositionV2> rawPosition, Guid existingPersonAzureId, Guid? newPersonAzureId, DateTime changeFrom)
                    {
                        var position = rawPosition.JsonObject.ToObject<ApiPositionV2>();
                        var rawInstances = rawPosition.GetPropertyCollection(p => p.Instances)!;

                        var instances = position!.Instances.Where(i => i.AssignedPerson?.AzureUniqueId == existingPersonAzureId && i.AppliesFrom > changeFrom).ToList();

                        ApiPersonV2? newAssignment = newPersonAzureId is null ? null : new ApiPersonV2() { AzureUniqueId = newPersonAzureId };

                        foreach (var instance in instances)
                        {
                            var instanceToUpdate = rawInstances
                                .Cast<JObject>()
                                .Select(x => new JObjectProxy<ApiPositionInstanceV2>(x))
                                .First(i => i.GetPropertyValue(p => p.Id) == instance.Id);
                            instanceToUpdate.SetPropertyValue(i => i.AssignedPerson, newAssignment!);
                        }
                    }
                }
            }
        }
    }
}
