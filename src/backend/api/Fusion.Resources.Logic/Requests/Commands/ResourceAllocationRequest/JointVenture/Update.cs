using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
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
        public partial class JointVenture
        {

            public class Update : TrackableRequest<QueryResourceAllocationRequest>
            {
                public Update(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }

                public MonitorableProperty<Guid?> OrgProjectId { get; private set; } = new();
                public MonitorableProperty<string?> AssignedDepartment { get; private set; } = new();
                public MonitorableProperty<string?> Discipline { get; private set; } = new();
                public MonitorableProperty<Guid?> OrgPositionId { get; private set; } = new();

                public MonitorableProperty<Domain.ResourceAllocationRequest.QueryPositionInstance> OrgPositionInstance
                {
                    get;
                    private set;
                } = new();

                public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; private set; } = new();
                public MonitorableProperty<string?> AdditionalNote { get; private set; } = new();
                public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; private set; } = new();
                public MonitorableProperty<bool> IsDraft { get; private set; } = new();

                public Update WithProjectId(Guid? projectId)
                {
                    if (projectId is not null) OrgProjectId = projectId;
                    return this;
                }

                public Update WithIsDraft(bool? isDraft)
                {
                    if (isDraft is not null) IsDraft = isDraft.GetValueOrDefault(true);
                    return this;
                }

                public Update WithAssignedDepartment(string? assignedDepartment)
                {
                    if (assignedDepartment is not null) AssignedDepartment = assignedDepartment;
                    return this;
                }

                public Update WithDiscipline(string? discipline)
                {
                    if (discipline is not null) Discipline = discipline;
                    return this;
                }

                public Update WithOrgPosition(Guid? orgPositionId)
                {
                    if (orgPositionId is not null) OrgPositionId = orgPositionId;
                    return this;
                }

                public Update WithProposedPerson(Guid? proposedPersonAzureUniqueId)
                {
                    if (proposedPersonAzureUniqueId is not null)
                        ProposedPersonAzureUniqueId = proposedPersonAzureUniqueId;
                    return this;
                }

                public Update WithAdditionalNode(string? note)
                {
                    if (note is not null) AdditionalNote = note;
                    return this;
                }

                public Update WithProposedChanges(Dictionary<string, object>? changes)
                {
                    if (changes is not null) ProposedChanges = changes;
                    return this;
                }

                public Update WithPositionInstance(Guid id, DateTime from, DateTime to, double? workload, string? obs,
                    Guid? locationId)
                {
                    OrgPositionInstance = new Domain.ResourceAllocationRequest.QueryPositionInstance
                    {
                        Id = id,
                        Workload = workload,
                        AppliesFrom = @from,
                        AppliesTo = to,
                        Obs = obs,
                        LocationId = locationId
                    };
                    return this;
                }

                public class Validator : AbstractValidator<Update>
                {
                    public Validator()
                    {
                        RuleFor(x => x.AssignedDepartment.Value).NotContainScriptTag().MaximumLength(500).When(x => x.AssignedDepartment.HasBeenSet);
                        RuleFor(x => x.Discipline.Value).NotContainScriptTag().MaximumLength(500).When(x => x.Discipline.HasBeenSet);
                        RuleFor(x => x.AdditionalNote.Value).NotContainScriptTag().MaximumLength(5000).When(x => x.AdditionalNote.HasBeenSet);
                        RuleFor(x => x.OrgPositionId.Value).NotEmpty().When(x => x.OrgPositionId.HasBeenSet && x.OrgPositionId.Value != null);
                        RuleFor(x => x.OrgPositionInstance).NotNull();
                        RuleFor(x => x.OrgPositionInstance.Value).BeValidPositionInstance().When(x => x.OrgPositionInstance != null);
                        RuleFor(x => x.ProposedChanges.Value).BeValidProposedChanges().When(x => x.ProposedChanges.HasBeenSet && x.ProposedChanges.Value != null);
                        RuleFor(x => x.ProposedPersonAzureUniqueId.Value).NotEmpty().When(x => x.ProposedPersonAzureUniqueId.HasBeenSet && x.ProposedPersonAzureUniqueId.Value != null);
                        RuleFor(x => x.OrgProjectId.Value).NotEmptyIfProvided();
                        RuleFor(x => x.IsDraft).NotNull();
                    }
                }

                public class Handler : IRequestHandler<Update, QueryResourceAllocationRequest
                >
                {
                    private readonly ResourcesDbContext db;
                    private readonly IMediator mediator;
                    private readonly IProjectOrgResolver orgResolver;
                    private readonly IProfileService profileService;
                    private DbPerson? ProposedPerson { get; set; }

                    public Handler(IProfileService profileService, IProjectOrgResolver orgResolver,
                        ResourcesDbContext db, IMediator mediator)
                    {
                        this.profileService = profileService;
                        this.orgResolver = orgResolver;
                        this.db = db;
                        this.mediator = mediator;
                    }

                    public async Task<QueryResourceAllocationRequest> Handle(Update request,
                        CancellationToken cancellationToken)
                    {
                        var dbEntity =
                            await db.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                        if (dbEntity is null)
                            throw new RequestNotFoundError(request.RequestId);


                        // Validate references.
                        await ValidateAsync(request);

                        var item = await PersistChangesAsync(request, dbEntity);

                        //TODO: Start the workflow. Workflow support to be implemented later...
                        //await mediator.Send(new Initialize(item.Id));

                        var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(item.Id));
                        return requestItem!;
                    }

                    private async Task<DbResourceAllocationRequest> PersistChangesAsync(Update request,
                        DbResourceAllocationRequest dbItem)
                    {
                        bool modified = false;
                        var updated = DateTimeOffset.UtcNow;

                        if (Project != null)
                        {
                            dbItem.Project = Project;
                            modified = true;
                        }

                        if (request.AssignedDepartment.HasBeenSet)
                        {
                            dbItem.AssignedDepartment = request.AssignedDepartment.Value;
                            modified = true;
                        }

                        if (request.Discipline.HasBeenSet)
                        {
                            dbItem.Discipline = request.Discipline.Value;
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
                            dbItem.OrgPositionId = request.OrgPositionId.Value;
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

                    private static string SerializeToString(Dictionary<string, object>? properties)
                    {
                        var propertiesJson = JsonSerializer.Serialize(properties ?? new Dictionary<string, object>(),
                            new JsonSerializerOptions
                            { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                        return propertiesJson;
                    }

                    private async Task ValidateAsync(Update request)
                    {
                        if (request.OrgProjectId.Value != null)
                        {
                            var project = await EnsureProjectAsync(request);
                            Project = project ?? throw new InvalidOperationException("Could not locate the project!");
                        }


                        if (request.ProposedPersonAzureUniqueId?.Value != null)
                        {
                            var proposed =
                                await profileService.EnsurePersonAsync(
                                    new PersonId(request.ProposedPersonAzureUniqueId.Value.Value));
                            ProposedPerson = proposed ?? throw new ProfileNotFoundError("Profile not found", null!);
                        }

                        await ValidateOriginalPositionAsync(request);
                    }

                    public DbProject? Project { get; set; }

                    private static DbResourceAllocationRequest.DbPositionInstance GenerateOrgPositionInstance(
                        Domain.ResourceAllocationRequest.QueryPositionInstance position)
                    {
                        return new DbResourceAllocationRequest.DbPositionInstance
                        {
                            AppliesFrom = position.AppliesFrom,
                            AppliesTo = position.AppliesTo,
                            Id = position.Id,
                            LocationId = position.LocationId,
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

                            if (request.OrgProjectId.Value != null &&
                                position.Project.ProjectId != request.OrgProjectId.Value)
                                throw InvalidOrgChartPositionError.InvalidProject(position);
                        }
                    }

                    private async Task<DbProject?> EnsureProjectAsync(Update request)
                    {
                        var orgProject = await orgResolver.ResolveProjectAsync(request.OrgProjectId.Value!.Value);
                        if (orgProject == null)
                            return null;

                        var project =
                            await db.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId.Value) ??
                            new DbProject
                            {
                                Name = orgProject.Name,
                                OrgProjectId = orgProject.ProjectId,
                                DomainId = orgProject.DomainId
                            };


                        return project;
                    }
                }
            }
        }
    }
}