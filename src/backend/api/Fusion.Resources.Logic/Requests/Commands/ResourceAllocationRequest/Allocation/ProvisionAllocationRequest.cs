using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Services.OrgClient;
using Fusion.Resources.Domain.Services.OrgClient.Models;
using Fusion.Services.Org.ApiModels;

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
                    private OrgApiClient client;
                    private readonly TelemetryClient telemetry;
                    private ResourcesDbContext resourcesDb;

                    public Handler(TelemetryClient telemetry, ResourcesDbContext resourcesDb, IOrgApiClientFactory orgApiClientFactory)
                    {
                        this.client = orgApiClientFactory.CreateClient();
                        this.telemetry = telemetry;
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
                        await EnsureDraftInitializedAsync(dbRequest, draft.Id);

                        await AllocateRequestPositionChangesAsync(dbRequest, draft, position);
                        await AllocateRequestInstanceAsync(dbRequest, draft, position);

                        await client.PublishAndWaitAsync(draft);
                    }

                    /// <summary>
                    /// From time to time the org service might take a while to initialize the draft. 
                    /// Ensure that the org service has initialized the draft by requesting the position. This should trigger the draft to start initializing. 
                    /// Give it a few retries to be fault tolerant.
                    /// </summary>
                    /// <param name="orgProjectId">The org chart id</param>
                    /// <param name="draftId">Draft to ensure initialized</param>
                    /// <param name="orgPositionId">Any existing position, should be the one we want to provision</param>
                    /// <exception cref="Exception"></exception>
                    private async Task EnsureDraftInitializedAsync(DbResourceAllocationRequest dbRequest, Guid draftId)
                    {
                        if (dbRequest.OrgPositionId is null)
                            throw new ArgumentNullException(nameof(dbRequest.OrgPositionId), "Request position id property cannot be null");

                        var orgProjectId = dbRequest.Project.OrgProjectId;
                        var orgPositionId = dbRequest.OrgPositionId.Value; 

                        var retriesCounter = 0;

                        while (true)
                        {
                            try
                            {
                                retriesCounter++;

                                var p = await client.GetAsync<ApiPositionV2>($"/projects/{orgProjectId}/drafts/{draftId}/positions/{orgPositionId}?api-version=2.0");

                                if (!p.IsSuccessStatusCode)
                                {
                                    telemetry.TrackTrace($"Response from org [{p.StatusCode}]: {p.Content}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
                                    throw new Exception($"Org api returned non successfull response when fetching position for draft initialization. Response code [{p.StatusCode}]");
                                }
                                
                                break;
                            }
                            catch (Exception e)
                            {
                                await Task.Delay(1000);

                                if (retriesCounter > 3)
                                {
                                    telemetry.TrackFusionCriticalEvent("Could not initialize draft for provisioning request", t => t
                                        .WithProperty("orgProjectId", $"{orgProjectId}")
                                        .WithProperty("orgPositionId", $"{orgPositionId}")
                                        .WithProperty("requestId", $"{dbRequest.Id}")
                                        .WithProperty("requestNumber", $"{dbRequest.RequestNumber}")
                                        .WithProperty("orgDraftId", $"{draftId}")
                                        .WithProperty("application", "FRA"));

                                    throw new Exception($"Could not initialize draft with id {draftId} on project {orgProjectId} for request {dbRequest.Id}", e);
                                }
                            }
                        }
                    }

                    private async Task<ApiDraftV2> CreateProvisionDraftAsync(DbResourceAllocationRequest dbRequest)
                    {
                        return await client.CreateProjectDraftAsync(dbRequest.Project.OrgProjectId, $"Allocation provisioning",
                            $"Provisioning of request [{dbRequest.Id}] to position [{dbRequest.OrgPositionId}/{dbRequest.OrgPositionInstance.Id}]");
                    }

                    private async Task AllocateRequestPositionChangesAsync(DbResourceAllocationRequest dbRequest, ApiDraftV2 draft, ApiPositionV2 position)
                    {
                        var positionPatchRequest = new JObject();
                        var proposedChanges = new JObject();

                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        try
                        {
                            if (proposedChanges.TryGetValue("basePosition", StringComparison.InvariantCultureIgnoreCase, out var basePosition) && basePosition.Type != JTokenType.Null)
                                positionPatchRequest.SetPropertyValue<ApiPositionV2>(p => p.BasePosition, basePosition.ToObject<ApiBasePositionV2>()!);
                        }
                        catch (Exception ex)
                        {
                            throw new ProvisioningError("Invalid data from request", ex);
                        }

                        if (positionPatchRequest.Count == 0)
                            return;

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/drafts/{draft.Id}/positions/{dbRequest.OrgPositionId}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionV2>(url, positionPatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);
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
                                instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocation>()!);
                        }
                        catch (Exception ex)
                        {
                            throw new ProvisioningError("Invalid data from request", ex);
                        }

                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/drafts/{draft.Id}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);
                    }
                }
            }
        }
    }
}
