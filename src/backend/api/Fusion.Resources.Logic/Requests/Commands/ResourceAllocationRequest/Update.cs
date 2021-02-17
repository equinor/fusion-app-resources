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
        public class Update : TrackableRequest<QueryResourceAllocationRequest>
        {
            public Update(Guid requestId)
            {
                RequestId = requestId;
            }
            public Guid RequestId { get; }

            public MonitorableProperty<Guid?> OrgProjectId { get; private set; } = new();
            public MonitorableProperty<string?> Discipline { get; private set; } = new();
            public MonitorableProperty<QueryResourceAllocationRequest.QueryAllocationRequestType> Type { get; private set; } = new();
            public MonitorableProperty<Guid?> OrgPositionId { get; private set; } = new();
            public MonitorableProperty<Domain.ResourceAllocationRequest.QueryPositionInstance> OrgPositionInstance { get; private set; } = new();
            public MonitorableProperty<Guid?> ProposedPersonAzureUniqueId { get; private set; } = new();
            public MonitorableProperty<string?> AdditionalNote { get; private set; } = new();
            public MonitorableProperty<Dictionary<string, object>?> ProposedChanges { get; private set; } = new();
            public MonitorableProperty<bool> IsDraft { get; private set; } = new();

            public Update WithProjectId(Guid? projectId)
            {
                OrgProjectId = projectId;
                return this;
            }

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

            public Update WithPositionInstance(Guid id, DateTime from, DateTime to, double? workload, string? obs, Guid? locationId)
            {
                var queryPositionInstance = new Domain.ResourceAllocationRequest.QueryPositionInstance
                {
                    Id = id,
                    Workload = workload,
                    AppliesFrom = @from,
                    AppliesTo = to,
                    Obs = obs,
                    LocationId = locationId
                };


                OrgPositionInstance = new();
                return this;
            }

            public class Validator : AbstractValidator<Update>
            {
                public Validator()
                {
                    RuleFor(x => x.Discipline.Value).NotContainScriptTag().MaximumLength(500).When(x => x.Discipline.HasBeenSet);
                    RuleFor(x => x.AdditionalNote.Value).NotContainScriptTag().MaximumLength(5000).When(x => x.Discipline.HasBeenSet);

                    RuleFor(x => x.OrgPositionId.Value).NotEmpty().When(x => x.OrgPositionId.HasBeenSet && x.OrgPositionId.Value != null);
                    RuleFor(x => x.OrgPositionInstance).NotNull();
                    RuleFor(x => x.OrgPositionInstance.Value).SetValidator(PositionInstanceValidator).When(x => x.OrgPositionInstance != null);
                    RuleFor(x => x.ProposedChanges.Value).SetValidator(ProposedChangesValidator).When(x => x.ProposedChanges.HasBeenSet && x.ProposedChanges.Value != null);

                    RuleFor(x => x.ProposedPersonAzureUniqueId.Value).NotEmpty().When(x => x.ProposedPersonAzureUniqueId.HasBeenSet && x.ProposedPersonAzureUniqueId.Value != null);

                    RuleFor(x => x.OrgProjectId.Value).NotEmptyIfProvided();
                    RuleFor(x => x.IsDraft).NotNull();
                }
                private static IPropertyValidator ProposedChangesValidator => new CustomValidator<Dictionary<string, object>>(
                    (prop, context) =>
                    {
                        foreach (var k in prop.Keys.Where(k => k.Length > 100))
                        {
                            context.AddFailure(new ValidationFailure($"{context.PropertyName}.key",
                                "Key cannot exceed 100 characters", k));
                        }

                    });

                private static IPropertyValidator PositionInstanceValidator => new CustomValidator<Domain.ResourceAllocationRequest.QueryPositionInstance>(
                    (position, context) =>
                    {
                        if (position == null) return;

                        if (position.AppliesTo < position.AppliesFrom)
                            context.AddFailure(new ValidationFailure($"{context.PropertyName}.appliesTo",
                                $"To date cannot be earlier than from date, {position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}",
                                $"{position.AppliesFrom:dd/MM/yyyy} -> {position.AppliesTo:dd/MM/yyyy}"));


                        if (position.Obs?.Length > 30)
                            context.AddFailure(new ValidationFailure($"{context.PropertyName}.obs",
                                "Obs cannot exceed 30 characters", position.Obs));

                        if (position.Workload < 0)
                            context.AddFailure(new ValidationFailure($"{context.PropertyName}.workload",
                                "Workload cannot be less than 0", position.Workload));

                        if (position.Workload > 100)
                            context.AddFailure(new ValidationFailure($"{context.PropertyName}.workload",
                                "Workload cannot be more than 100", position.Workload));
                    });
            }

            public class Handler : IRequestHandler<Update, QueryResourceAllocationRequest
            >
            {
                private readonly ResourcesDbContext db;
                private readonly IMediator mediator;
                private readonly IProjectOrgResolver orgResolver;
                private readonly IProfileService profileService;
                private DbPerson? ProposedPerson { get; set; }
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

                    var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(item.Id));
                    return requestItem!;
                }

                private async Task<DbResourceAllocationRequest> PersistChangesAsync(Update request, DbResourceAllocationRequest dbItem)
                {
                    bool modified = false;
                    var updated = DateTimeOffset.UtcNow;

                    if (Project != null)
                    {
                        dbItem.Project = Project;
                        modified = true;
                    }
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
                    if (request.OrgProjectId.Value != null)
                    {
                        var project = await EnsureProjectAsync(request);
                        Project = project ?? throw new InvalidOperationException("Could not locate the project!");
                    }


                    if (request.ProposedPersonAzureUniqueId?.Value != null)
                    {
                        var proposed = await profileService.EnsurePersonAsync(new PersonId(request.ProposedPersonAzureUniqueId.Value.Value));
                        ProposedPerson = proposed ?? throw new ProfileNotFoundError("Profile not found", null!);
                    }

                    await ValidateOriginalPositionAsync(request);
                }

                public DbProject? Project { get; set; }

                private static DbResourceAllocationRequest.DbPositionInstance GenerateOrgPositionInstance(Domain.ResourceAllocationRequest.QueryPositionInstance position)
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

                        if (request.OrgProjectId.Value != null && position.Project.ProjectId != request.OrgProjectId.Value)
                            throw InvalidOrgChartPositionError.InvalidProject(position);
                    }
                }
                private async Task<DbProject?> EnsureProjectAsync(Update request)
                {
                    var orgProject = await orgResolver.ResolveProjectAsync(request.OrgProjectId.Value!.Value);
                    if (orgProject == null)
                        return null;

                    var project = await db.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId.Value) ?? new DbProject
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