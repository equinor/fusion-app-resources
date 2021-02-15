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
        public class Create : TrackableRequest<QueryResourceAllocationRequest>
        {
            public Create(Guid orgProjectId)
            {
                OrgProjectId = orgProjectId;
            }

            private Guid OrgProjectId { get; }

            public string? Discipline { get; private set; }
            public QueryResourceAllocationRequest.QueryAllocationRequestType Type { get; private set; }

            public Guid? OrgPositionId { get; private set; }

            public Domain.ResourceAllocationRequest.QueryPositionInstance OrgPositionInstance { get; private set; } = null!;

            public Guid? ProposedPersonAzureUniqueId { get; private set; }
            public string? AdditionalNote { get; private set; }
            public Dictionary<string, object>? ProposedChanges { get; private set; }
            public bool IsDraft { get; private set; }


            public Create WithIsDraft(bool? isDraft)
            {
                IsDraft = isDraft.GetValueOrDefault(true);
                return this;
            }

            public Create WithDiscipline(string? discipline)
            {
                Discipline = discipline;
                return this;
            }

            public Create WithType(string type)
            {
                Type = Enum.Parse<QueryResourceAllocationRequest.QueryAllocationRequestType>(type);
                return this;
            }

            public Create WithOrgPosition(Guid? originalPositionId)
            {
                OrgPositionId = originalPositionId;
                return this;
            }

            public Create WithProposedPerson(Guid? proposedPersonAzureUniqueId)
            {
                ProposedPersonAzureUniqueId = proposedPersonAzureUniqueId;
                return this;
            }

            public Create WithAdditionalNode(string? note)
            {
                AdditionalNote = note;
                return this;
            }
            public Create WithProposedChanges(Dictionary<string, object>? changes)
            {
                ProposedChanges = changes;
                return this;
            }

            public Create WithPositionInstance(Guid basePositionId, DateTime from, DateTime to, double? workload, string? obs, Guid? locationId)
            {
                OrgPositionInstance = new Domain.ResourceAllocationRequest.QueryPositionInstance
                {
                    Id = basePositionId,
                    Workload = workload,
                    AppliesFrom = @from,
                    AppliesTo = to,
                    Obs = obs,
                    LocationId = locationId
                };

                return this;
            }
            public class Validator : AbstractValidator<Create>
            {
                public Validator()
                {
                    RuleFor(x => x.Discipline).NotContainScriptTag().MaximumLength(500);
                    RuleFor(x => x.AdditionalNote).NotContainScriptTag().MaximumLength(5000);

                    RuleFor(x => x.OrgPositionId).NotEmpty().When(x => x.OrgPositionId != null);
                    RuleFor(x => x.OrgPositionInstance).NotNull();
                    RuleFor(x => x.OrgPositionInstance).SetValidator(PositionInstanceValidator).When(x => x.OrgPositionInstance != null);
                    RuleFor(x => x.ProposedChanges).SetValidator(ProposedChangesValidator).When(x => x.ProposedChanges != null);

                    RuleFor(x => x.ProposedPersonAzureUniqueId).NotEmpty().When(x => x.ProposedPersonAzureUniqueId != null);

                    RuleFor(x => x.OrgProjectId).NotNull();
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

            public class Handler : IRequestHandler<Create, QueryResourceAllocationRequest
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

                private DbProject? Project { get; set; }

                public async Task<QueryResourceAllocationRequest> Handle(Create request, CancellationToken cancellationToken)
                {
                    // Validate references.
                    await ValidateAsync(request);

                    var item = await PersistChangesAsync(request);

                    //TODO: Start the workflow. Workflow support to be implemented later...
                    //await mediator.Send(new Initialize(item.Id));


                    var dbRequest = await mediator.Send(new GetResourceAllocationRequestItem(item.Id));
                    return dbRequest;
                }

                private async Task<DbResourceAllocationRequest> PersistChangesAsync(Create request)
                {
                    var created = DateTimeOffset.UtcNow;

                    var item = new DbResourceAllocationRequest
                    {
                        Id = Guid.NewGuid(),
                        Discipline = request.Discipline,
                        Type = ParseRequestType(request),
                        State = DbRequestState.Created,

                        Project = Project!,

                        ProposedPerson = ProposedPerson,
                        AdditionalNote = request.AdditionalNote,

                        ProposedChanges = SerializeToString(request.ProposedChanges),

                        OrgPositionId = request.OrgPositionId,
                        OrgPositionInstance = GenerateOrgPositionInstance(request.OrgPositionInstance)!,

                        IsDraft = request.IsDraft,

                        Created = created,
                        CreatedBy = request.Editor.Person,
                        LastActivity = created,

                        ProposedPersonWasNotified = false, // Should be set/reset during update when/if when notifications are enabled

                    };

                    await db.ResourceAllocationRequests.AddAsync(item);
                    await db.SaveChangesAsync();

                    return item;
                }

                private static DbResourceAllocationRequest.DbAllocationRequestType ParseRequestType(Create request)
                {
                    return Enum.Parse<DbResourceAllocationRequest.DbAllocationRequestType>($"{request.Type}");
                }

                private static string SerializeToString(Dictionary<string, object>? properties)
                {
                    var propertiesJson = JsonSerializer.Serialize(properties ?? new Dictionary<string, object>(), new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    return propertiesJson;
                }
                private async Task ValidateAsync(Create request)
                {
                    if (request.ProposedPersonAzureUniqueId != null)
                    {
                        var proposed = await profileService.EnsurePersonAsync(new PersonId(request.ProposedPersonAzureUniqueId.Value));
                        ProposedPerson = proposed ?? throw new InvalidOperationException("Profile not found");
                    }

                    var project = await EnsureProjectAsync(request);
                    Project = project ?? throw new InvalidOperationException("Could not locate the project!");

                    await ValidateOriginalPositionAsync(request);
                }

                private static DbResourceAllocationRequest.DbPositionInstance? GenerateOrgPositionInstance(Domain.ResourceAllocationRequest.QueryPositionInstance position)
                {
                    if (position == null)
                        return null;
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

                private async Task ValidateOriginalPositionAsync(Create request)
                {
                    if (request.OrgPositionId != null)
                    {
                        var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value);

                        if (position is null)
                            throw InvalidOrgChartPositionError.NotFound(request.OrgPositionId.Value);

                        if (position.Project.ProjectId != request.OrgProjectId)
                            throw InvalidOrgChartPositionError.InvalidProject(position);
                    }
                }
                private async Task<DbProject?> EnsureProjectAsync(Create request)
                {
                    var orgProject = await orgResolver.ResolveProjectAsync(request.OrgProjectId);
                    if (orgProject == null)
                        return null;

                    var project = await db.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId) ?? new DbProject
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