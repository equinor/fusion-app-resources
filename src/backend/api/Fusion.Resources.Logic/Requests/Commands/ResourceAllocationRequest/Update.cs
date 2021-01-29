using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class Update : TrackableRequest<QueryResourceAllocationRequest>
        {
            public Update(Guid orgProjectId, Guid requestId)
            {
                OrgProjectId = orgProjectId;
                RequestId = requestId;
            }
            private Guid OrgProjectId { get; }
            private Guid RequestId { get; }

            public MonitorableProperty<string?> Discipline { get; private set; } = new MonitorableProperty<string?>();

            public MonitorableProperty<QueryResourceAllocationRequest.QueryAllocationRequestType> Type { get; private set; } = new MonitorableProperty<QueryResourceAllocationRequest.QueryAllocationRequestType>();
            public MonitorableProperty<Guid?> OrgPositionId { get; private set; } = new MonitorableProperty<Guid?>();

            public MonitorableProperty<Domain.ResourceAllocationRequest.QueryPositionInstance> OrgPositionInstance { get; private set; }

            public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; private set; } = new MonitorableProperty<Guid?>();
            public MonitorableProperty<string?> AdditionalNote { get; private set; } = new MonitorableProperty<string?>();

            public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; private set; } =
                new MonitorableProperty<Dictionary<string, object>?>();

            public MonitorableProperty<bool> IsDraft { get; private set; } = new MonitorableProperty<bool>();


            public Update WithIsDraft(bool? isDraft)
            {
                IsDraft = isDraft.GetValueOrDefault(true);
                return this;
            }

            public Update WithDiscipline(string? discipline)
            {
                Discipline = discipline;
                return this;
            }

            public Update WithType(string type)
            {
                Type = Enum.Parse<QueryResourceAllocationRequest.QueryAllocationRequestType>(type);
                return this;
            }

            public Update WithOrgPosition(Guid? originalPositionId)
            {
                OrgPositionId = originalPositionId;
                return this;
            }

            public Update WithProposedPerson(Guid? proposedPersonAzureUniqueId)
            {
                ProposedPersonAzureUniqueId = proposedPersonAzureUniqueId;
                return this;
            }

            public Update WithAdditionalNode(string? note)
            {
                AdditionalNote = note;
                return this;
            }
            public Update WithProposedChanges(Dictionary<string, object>? changes)
            {
                ProposedChanges = changes;
                return this;
            }

            public Update WithPositionInstance(Guid id, DateTime from, DateTime to, double workload, string? obs, string location)
            {
                var queryPositionInstance = new Domain.ResourceAllocationRequest.QueryPositionInstance
                {
                    Id = id,
                    Workload = workload,
                    AppliesFrom = @from,
                    AppliesTo = to,
                    Obs = obs ?? string.Empty,
                    Location = location
                };


                OrgPositionInstance =
                    new MonitorableProperty<Domain.ResourceAllocationRequest.QueryPositionInstance>(queryPositionInstance);
                return this;
            }

            public class Handler : IRequestHandler<Update, QueryResourceAllocationRequest
            >
            {
                private readonly ResourcesDbContext db;
                private readonly IMediator mediator;
                private readonly IProjectOrgResolver orgResolver;
                private readonly IProfileService profileService;
                private DbPerson ProposedPerson { get; set; }
                public Handler(IProfileService profileService, IProjectOrgResolver orgResolver, ResourcesDbContext db, IMediator mediator)
                {
                    this.profileService = profileService;
                    this.orgResolver = orgResolver;
                    this.db = db;
                    this.mediator = mediator;
                }
                public async Task<QueryResourceAllocationRequest> Handle(Update request, CancellationToken cancellationToken)
                {
                    var dbEntity = await db.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                    if (dbEntity is null)
                        throw new RequestNotFoundError(request.RequestId);


                    // Validate references.
                    await ValidateAsync(request);

                    var item = await PersistChangesAsync(request, dbEntity);

                    //TODO: Start the workflow. Workflow support to be implemented later...
                    //await mediator.Send(new Initialize(item.Id));

                    return await mediator.Send(new GetProjectResourceAllocationRequestItem(item.Id));
                }

                private async Task<DbResourceAllocationRequest> PersistChangesAsync(Update request, DbResourceAllocationRequest dbItem)
                {
                    bool modified = false;
                    var updated = DateTimeOffset.UtcNow;

                    if (request.Discipline.HasBeenSet)
                    {
                        dbItem.Discipline = request.Discipline.Value;
                        modified = true;
                    }

                    if (request.Type.HasBeenSet)
                    {
                        dbItem.Type = ParseRequestType(request);
                        modified = true;
                    }

                    if (request.ProposedChanges.HasBeenSet)
                    {
                        dbItem.ProposedPerson = ProposedPerson;
                        modified = true;
                    }

                    if (request.AdditionalNote.HasBeenSet)
                    {
                        dbItem.AdditionalNote = request.AdditionalNote.Value;
                        modified = true;
                    }

                    if (request.ProposedChanges.HasBeenSet)
                    {
                        dbItem.ProposedChanges = SerializeToString(request.ProposedChanges.Value);
                        modified = true;
                    }

                    if (request.OrgPositionInstance.HasBeenSet)
                    {
                        dbItem.OriginalPositionId = request.OrgPositionId.Value;
                        modified = true;
                    }

                    if (request.OrgPositionInstance.HasBeenSet)
                    {
                        dbItem.OrgPositionInstance = GenerateOrgPositionInstance(request.OrgPositionInstance.Value);
                        modified = true;
                    }

                    if (request.IsDraft.HasBeenSet)
                    {
                        dbItem.IsDraft = request.IsDraft.Value;
                        modified = true;
                    }

                    /*
                    {
                        dbItem.ProposedPersonWasNotified =  // Should be set/reset during update when/if when notifications are enabled
                        modified = true;
                    }
                    */

                    if (modified)
                    {
                        dbItem.Updated = updated;
                        dbItem.UpdatedBy = request.Editor.Person;
                        dbItem.LastActivity = updated;

                        await db.SaveChangesAsync();
                    }

                    return dbItem;
                }

                private static DbResourceAllocationRequest.DbAllocationRequestType ParseRequestType(Update request)
                {
                    return Enum.Parse<DbResourceAllocationRequest.DbAllocationRequestType>($"{request.Type.Value}");
                }

                private static string SerializeToString(Dictionary<string, object>? properties)
                {
                    var propertiesJson = JsonSerializer.Serialize(properties ?? new Dictionary<string, object>(), new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    return propertiesJson;
                }
                private async Task ValidateAsync(Update request)
                {
                    if (request.ProposedPersonAzureUniqueId?.Value != null)
                    {
                        var proposed = await profileService.EnsurePersonAsync(new PersonId(request.ProposedPersonAzureUniqueId.Value.Value));
                        ProposedPerson = proposed ?? throw new ProfileNotFoundError("Profile not found", null);
                    }

                    await ValidateOriginalPositionAsync(request);
                }

                private static DbResourceAllocationRequest.DbPositionInstance GenerateOrgPositionInstance(Domain.ResourceAllocationRequest.QueryPositionInstance position)
                {
                    return new DbResourceAllocationRequest.DbPositionInstance
                    {
                        AppliesFrom = position.AppliesFrom,
                        AppliesTo = position.AppliesTo,
                        Id = position.Id,
                        Location = position.Location,
                        Workload = position.Workload,
                        Obs = position.Obs
                    };
                }

                private async Task ValidateOriginalPositionAsync(Update request)
                {
                    if (request.OrgPositionId.Value != null)
                    {
                        var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value.Value);

                        if (position is null)
                            throw InvalidOrgChartPositionError.NotFound(request.OrgPositionId.Value.Value);

                        if (position.Project.ProjectId != request.OrgProjectId)
                            throw InvalidOrgChartPositionError.InvalidProject(position);
                    }
                }
            }
        }
    }
}