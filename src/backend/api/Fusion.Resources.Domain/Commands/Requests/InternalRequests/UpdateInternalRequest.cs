using Fusion.Resources.Database;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Newtonsoft.Json;
using Fusion.Resources.Database.Entities;
using Newtonsoft.Json.Linq;

namespace Fusion.Resources.Domain.Commands
{

    public class UpdateInternalRequest : TrackableRequest<QueryResourceAllocationRequest>
    {
        public UpdateInternalRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public MonitorableProperty<string?> AssignedDepartment { get; set; } = new();
        public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; set; } = new();
        public MonitorableProperty<string?> AdditionalNote { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> Properties { get; set; } = new();
        public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; set; } = new();

        public MonitorableProperty<DateTime?> ProposalChangeFrom { get; set; } = new();
        public MonitorableProperty<DateTime?> ProposalChangeTo { get; set; } = new();
        public MonitorableProperty<ProposalChangeScope> ProposalScope { get; set; } = new();

        // Placeholder, not used currently
        public MonitorableProperty<string?> ProposalChangeType { get; set; } = new();

        public MonitorableProperty<List<PersonId>> Candidates { get; set; } = new();


        /// <summary>
        /// Unassign the request. Set assigned department to null.
        /// 
        /// Factory to create unassign command more reable.
        /// </summary>
        /// <param name="requestId">The request to update</param>
        /// <returns>Dispatchable command</returns>
        public static UpdateInternalRequest UnassignRequest(Guid requestId)
        {
            var cmd = new UpdateInternalRequest(requestId);
            cmd.AssignedDepartment = new MonitorableProperty<string?>(null);

            return cmd;
        }

        public class Handler : IRequestHandler<UpdateInternalRequest, QueryResourceAllocationRequest>
        {
            private readonly ResourcesDbContext db;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProfileService profileService, IMediator mediator, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.profileService = profileService;
                this.mediator = mediator;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryResourceAllocationRequest> Handle(UpdateInternalRequest request, CancellationToken cancellationToken)
            {
                var dbRequest = await db.ResourceAllocationRequests.FirstAsync(r => r.Id == request.RequestId);

                bool modified = false;

                modified |= request.AdditionalNote.IfSet(note => dbRequest.AdditionalNote = note);
                modified |= await request.ProposedChanges.IfSetAsync(async changes =>
                {
                    await EnsureProposedBasePositionExists(changes);
                    dbRequest.ProposedChanges = changes.SerializeToStringOrDefault();
                });

                modified |= await request.AssignedDepartment.IfSetAsync(async dep => await UpdateAssignedOrgUnitAsync(dep, dbRequest));

                modified |= await request.Properties.IfSetAsync(async properties =>
                {
                    if (properties is not null)
                    {
                        var resolvedProperties = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                        var existingProps = new Dictionary<string, object>();
                        if (string.IsNullOrEmpty(resolvedProperties?.PropertiesJson) == false)
                        {
                            existingProps = JsonConvert.DeserializeObject<Dictionary<string, object>>(resolvedProperties.PropertiesJson) ?? new Dictionary<string, object>();
                        }
                        foreach (var property in properties)
                        {

                            if (property.Value == null || string.IsNullOrEmpty(property.Value?.ToString()))
                            {
                                existingProps.Remove(property.Key);
                            }
                            else
                            {
                                existingProps[property.Key] = property.Value;
                            }
                        }
                        dbRequest.Properties = existingProps.SerializeToStringOrDefault();
                    }
                });
                modified |= await request.ProposedPersonAzureUniqueId.IfSetAsync(async personId =>
                    {
                        if (personId is not null)
                        {
                            var resolvedPerson = await profileService.EnsurePersonAsync(new PersonId(personId.Value));
                            dbRequest.ProposePerson(resolvedPerson!);
                        }
                        else
                        {
                            dbRequest.ProposedPerson.Clear();
                        }
                    });
                modified |= await request.Candidates.IfSetAsync(async candidates =>
                {
                    dbRequest.Candidates.Clear();
                    foreach (var personId in candidates)
                    {
                        var resolvedPerson = await profileService.EnsurePersonAsync(personId);
                        if (resolvedPerson is null) throw new Exception();

                        dbRequest.Candidates.Add(resolvedPerson);
                    }

                    if (dbRequest.Candidates.Count == 1 && !dbRequest.ProposedPerson.HasBeenProposed)
                    {
                        dbRequest.ProposePerson(dbRequest.Candidates.Single());
                    }
                });
                modified |= request.ProposalChangeFrom.IfSet(dt => dbRequest.ProposalParameters.ChangeFrom = dt);
                modified |= request.ProposalChangeTo.IfSet(dt => dbRequest.ProposalParameters.ChangeTo = dt);
                modified |= request.ProposalChangeType.IfSet(dt => dbRequest.ProposalParameters.ChangeType = dt);
                modified |= request.ProposalScope.IfSet(dt => dbRequest.ProposalParameters.Scope = dt.MapToDatabase());

                if (modified)
                {
                    dbRequest.Updated = DateTimeOffset.UtcNow;
                    dbRequest.UpdatedBy = request.Editor.Person;
                    dbRequest.LastActivity = dbRequest.Updated.Value;

                    var modifiedProperties = db.Entry(dbRequest).Properties.Where(x => x.IsModified).ToList();

                    await db.SaveChangesAsync(cancellationToken);

                    await mediator.Publish(new InternalRequestUpdated(dbRequest.Id, modifiedProperties));
                }

                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId));
                return requestItem!;
            }

            private async Task UpdateAssignedOrgUnitAsync(string? assignedDepartment, DbResourceAllocationRequest dbItem)
            {
                if (!string.IsNullOrEmpty(assignedDepartment))
                {
                    var orgUnit = await mediator.Send(new ResolveLineOrgUnit(assignedDepartment));

                    // If the assigned department is provided as input, it should be validated that it exists.
                    if (orgUnit is null)
                    {
                        throw new InvalidOperationException($"Could not resolve org unit using identifier '{assignedDepartment}'. Unable to set assigned department");
                    }

                    dbItem.AssignedDepartment = orgUnit.FullDepartment;
                    dbItem.AssignedDepartmentId = orgUnit.SapId;
                } 
                else
                {
                    dbItem.AssignedDepartment = null;
                    dbItem.AssignedDepartmentId = null;
                }
            }

            private async Task EnsureProposedBasePositionExists(Dictionary<string, object>? proposedChanges)
            {
                if (proposedChanges == null)
                    return;
                var jObject = JObject.FromObject(proposedChanges);

                if (!jObject.TryGetValue("basePosition", StringComparison.InvariantCultureIgnoreCase, out var basePositionJToken) || basePositionJToken.Type == JTokenType.Null)
                    return;

                ApiBasePositionV2? basePosition;
                try
                {
                    basePosition = basePositionJToken.ToObject<ApiBasePositionV2>()!;
                }
                catch (Exception)
                {
                    throw new ValidationException("Could not parse proposed base position");
                }

                bool basePositionExists;
                try
                {
                    basePositionExists = await orgResolver.ResolveBasePositionAsync(basePosition.Id) != null;
                }
                catch (Exception e)
                {
                    throw new IntegrationError("Could not resolve proposed base position", e);
                }

                if (!basePositionExists)
                    throw new ValidationException($"Base position with id {basePosition.Id} does not exist");
            }
        }

    }
}
