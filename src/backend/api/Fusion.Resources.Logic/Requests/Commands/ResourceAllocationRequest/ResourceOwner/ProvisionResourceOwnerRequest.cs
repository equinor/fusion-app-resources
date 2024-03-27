using Fusion.ApiClients.Org;
using Fusion.Integration.Configuration;
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
        public partial class ResourceOwner
        {

            internal class ProvisionResourceOwnerRequest : IRequest
            {
                public ProvisionResourceOwnerRequest(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }


                public class Handler : IRequestHandler<ProvisionResourceOwnerRequest>
                {
                    private readonly IOrgApiClient client;
                    private readonly IOrgHttpClient httpClient;
                    private readonly ResourcesDbContext resourcesDb;

                    public Handler(ResourcesDbContext resourcesDb, IOrgApiClientFactory orgApiClientFactory, IOrgHttpClient httpClient)
                    {
                        this.client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                        this.httpClient = httpClient;
                        this.resourcesDb = resourcesDb;
                    }

                    public async Task Handle(ProvisionResourceOwnerRequest request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                        if (dbRequest != null) 
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

                        if (isFuture && isSameDates)
                            await UpdateFutureSplitAsync(dbRequest, rawPosition.Value);
                        else
                        {
                            var equalStart = effectiveChangeFrom.Date == positionInstance.AppliesFrom.Date;
                            var equalEnd = effectiveChangeTo.Date == positionInstance.AppliesTo.Date;

                            var jsonPosition = rawPosition.Value;

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

                    private void UpdateSuccessorInstances(DbResourceAllocationRequest dbRequest, JObject rawPosition, Guid? assignedPersonAzureId)
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

                    private async Task SavePositionAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition)
                    {
                        string url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        string json = rawPosition.ToString(Formatting.None);
                        HttpContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(HttpMethod.Put, url);
                        request.Content = content;

                        var resp
                            = await httpClient.SendAsync<JObject>(request);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }

                    private void UpdateStart(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeTo)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var newInstanceStartDate = changeTo.Date;
                        var existingInstanceEndDate = changeTo.Date.AddDays(-1);


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
                    }

                    private void UpdateEnd(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeFrom)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;


                        var originalInstanceEndDate = changeFrom.Date.AddDays(-1);
                        var newInstanceStartDate = changeFrom.Date;

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

                    }

                    private void UpdateCenter(DbResourceAllocationRequest dbRequest, JObject rawPosition, DateTime changeFrom, DateTime changeTo)
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


                        var existingInstanceEndDate = changeFrom.Date.AddDays(-1);
                        var trailingInstanceStartDate = changeTo.Date.AddDays(1);


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
                    }

                    private void ApplyProposedChanges(DbResourceAllocationRequest dbRequest, JObject instance)
                    {
                        var subType = new SubType(dbRequest.SubType);
                        
                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges) && dbRequest.ProposedChanges != "null")
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        switch (subType.Value)
                        {
                            case SubType.Types.Adjustment:
                                if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                                    instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                                if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                                    instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);
                                break;

                            case SubType.Types.ChangeResource:
                                if (dbRequest.ProposedPerson.AzureUniqueId != null)
                                    instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });
                                break;

                            case SubType.Types.RemoveResource:
                                instance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, null!);
                                break;
                        }
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

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

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
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";

                        string json = rawPosition.ToString(Formatting.None);
                        HttpContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(HttpMethod.Patch, url);
                        request.Content = content;

                        var updateResp
                            = await httpClient.SendAsync<ApiPositionInstanceV2>(request);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);

                    }

                    private void UpdateResourceOnInstancesAfter(JObject rawPosition, Guid existingPersonAzureId, Guid? newPersonAzureId, DateTime changeFrom)
                    {
                        var position = rawPosition.ToObject<ApiPositionV2>();
                        var rawInstances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var instances = position!.Instances.Where(i => i.AssignedPerson?.AzureUniqueId == existingPersonAzureId && i.AppliesFrom > changeFrom).ToList();

                        ApiPersonV2? newAssignment = newPersonAzureId is null ? null : new ApiPersonV2() { AzureUniqueId = newPersonAzureId };

                        foreach (var instance in instances)
                        {
                            var instanceToUpdate = rawInstances.Cast<JObject>().First(i => i.GetPropertyValue<ApiPositionInstanceV2, Guid>(p => p.Id) == instance.Id);
                            instanceToUpdate.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, newAssignment!);
                        }
                    }
                }
            }
        }
    }
}
