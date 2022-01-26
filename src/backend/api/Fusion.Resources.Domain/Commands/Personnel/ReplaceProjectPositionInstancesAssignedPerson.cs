using MediatR;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;

namespace Fusion.Resources.Domain.Commands
{
    public class ReplaceProjectPositionInstancesAssignedPerson : TrackableRequest
    {
        public ReplaceProjectPositionInstancesAssignedPerson(Guid projectId, Guid contractIdentifier, PersonnelId fromPerson, PersonnelId toPerson)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            FromPerson = fromPerson;
            ToPerson = toPerson;
        }
        public Guid OrgContractId { get; }
        public Guid OrgProjectId { get; }
        public PersonnelId FromPerson { get; }
        public PersonnelId ToPerson { get; }

        public class Handler : AsyncRequestHandler<ReplaceProjectPositionInstancesAssignedPerson>
        {
            private readonly IOrgApiClient client;

            public Handler(IOrgApiClientFactory orgApi)
            {

                this.client = orgApi.CreateClient(ApiClientMode.Application);
            }

            protected override async Task Handle(ReplaceProjectPositionInstancesAssignedPerson request, CancellationToken cancellationToken)
            {
                var draft = await CreateProvisionDraftAsync(request);

                var positions = await client.GetContractPositionsV2Async(request.OrgProjectId, request.OrgContractId);

                await ReplaceAssignedPersonOnRelevantPositionInstancesInDraftAsync(request, draft, positions);

                await client.PublishAndWaitAsync(draft);
            }

            private async Task<ApiDraftV2> CreateProvisionDraftAsync(ReplaceProjectPositionInstancesAssignedPerson request)
            {
                return await client.CreateProjectDraftAsync(request.OrgProjectId, $"Assigned Person Replacement",
                    $"Replacing assigned person from person [{request.FromPerson.OriginalIdentifier}] to person [{request.ToPerson.OriginalIdentifier}] on position instances on project[{request.OrgProjectId}], contract[{request.OrgContractId}]");
            }

            private async Task ReplaceAssignedPersonOnRelevantPositionInstancesInDraftAsync(ReplaceProjectPositionInstancesAssignedPerson request, ApiDraftV2 draft, List<ApiPositionV2> positions)
            {
                if (request.FromPerson.UniqueId is null || request.ToPerson.UniqueId is null)
                    throw new InvalidOperationException("Cannot update position instance without existing from-/to- person arguments.");

                foreach (var position in positions)
                {
                    var isModified = false;

                    foreach (var instance in position.Instances.Where(y => y.AssignedPerson?.AzureUniqueId == request.FromPerson.UniqueId))
                    {
                        instance.AssignedPerson = new ApiPersonV2 { AzureUniqueId = request.ToPerson.UniqueId };
                        isModified = true;
                    }

                    if (!isModified) continue;

                    var url = $"/projects/{request.OrgProjectId}/drafts/{draft.Id}/contracts/{request.OrgContractId}/positions/{position.Id}?api-version=2.0";
                    var updateResp = await client.PatchAsync<ApiPositionV2>(url, position);

                    if (!updateResp.IsSuccessStatusCode)
                        throw new OrgApiError(updateResp.Response, updateResp.Content);

                }
            }
        }
    }
}
