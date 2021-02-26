using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class JointVenture
        {
            public class Create : TrackableRequest<QueryResourceAllocationRequest>
            {
                public Create(Guid orgProjectId)
                {
                    OrgProjectId = orgProjectId;
                }

                public Guid OrgProjectId { get; }
                public string? AssignedDepartment { get; private set; }
                public string? Discipline { get; private set; }
                public QueryResourceAllocationRequest.QueryAllocationRequestType Type { get; private set; }
                public Guid? OrgPositionId { get; private set; }
                public Guid? OrgPositionInstanceId { get; private set; }
                public Guid? ProposedPersonAzureUniqueId { get; private set; }
                public string? AdditionalNote { get; private set; }
                public Dictionary<string, object>? ProposedChanges { get; private set; }
                public bool IsDraft { get; private set; }


                public Create WithIsDraft(bool? isDraft)
                {
                    IsDraft = isDraft.GetValueOrDefault(true);
                    return this;
                }

                public Create WithAssignedDepartment(string? assignedDepartment)
                {
                    AssignedDepartment = assignedDepartment;
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
                public Create WithOrgPositionId(Guid? id)
                {
                    OrgPositionId = id;
                    return this;
                }
                public Create WithOrgPositionInstanceId(Guid? id)
                {
                    OrgPositionInstanceId = id;
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

                public class Validator : AbstractValidator<Create>
                {
                    public Validator(IProjectOrgResolver orgResolver, IProfileService profileService, ResourcesDbContext db)
                    {
                        RuleFor(x => x.OrgPositionId).MustAsync(async (id, cancel) =>
                        {
                            var position = await orgResolver.ResolvePositionAsync(id!.Value);
                            return position != null;

                        }).WithMessage("Position must exist in org");
                        RuleFor(x => x.OrgPositionId).MustAsync(async (id, cancel) =>
                        {
                            var positionRequest = await db.ResourceAllocationRequests.FirstOrDefaultAsync(y => y.OrgPositionId == id);
                            return positionRequest == null;
                        }).WithMessage("Request for org position must not exist");
                        RuleFor(x => x.ProposedChanges).BeValidProposedChanges().When(x => x.ProposedChanges != null);

                        RuleFor(x => x.ProposedPersonAzureUniqueId).MustAsync(async (id, cancel) =>
                            {
                                var profile = await profileService.EnsurePersonAsync(new PersonId(id!.Value));
                                return profile != null;

                            }).WithMessage("Profile must exist in profile service")
                            .When(x => x.ProposedPersonAzureUniqueId != null);

                        RuleFor(x => x.OrgProjectId).MustAsync(async (id, cancel) =>
                        {
                            var orgProject = await orgResolver.ResolveProjectAsync(id);
                            return orgProject != null;

                        }).WithMessage("Project must exist in org");
                    }
                }

                public class Handler : IRequestHandler<Create, QueryResourceAllocationRequest>
                {
                    private readonly ResourcesDbContext db;
                    private readonly IMediator mediator;
                    private readonly IProjectOrgResolver orgResolver;
                    private readonly IProfileService profileService;
                    
                    public Handler(IProfileService profileService, IProjectOrgResolver orgResolver, ResourcesDbContext db, IMediator mediator)
                    {
                        this.profileService = profileService;
                        this.orgResolver = orgResolver;
                        this.db = db;
                        this.mediator = mediator;
                    }

                    public async Task<QueryResourceAllocationRequest> Handle(Create request, CancellationToken cancellationToken)
                    {
                       
                        var item = await PersistChangesAsync(request);

                        var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(item.Id));
                        return requestItem!;
                    }


                    private async Task<DbResourceAllocationRequest> PersistChangesAsync(Create request)
                    {
                        var created = DateTimeOffset.UtcNow;
                        var resolvedProject = await EnsureProjectAsync(request);
                        await ValidateOrgPositionAsync(request);

                        DbPerson? proposedPerson = null;
                        if (request.ProposedPersonAzureUniqueId != null)
                            proposedPerson = await profileService.EnsurePersonAsync(new PersonId(request.ProposedPersonAzureUniqueId.Value));

                        var item = new DbResourceAllocationRequest
                        {
                            Id = Guid.NewGuid(),
                            AssignedDepartment = request.AssignedDepartment,
                            Discipline = request.Discipline,
                            Type = ParseRequestType(request),
                            State = DbResourceAllocationRequestState.Created,

                            Project = resolvedProject!,

                            ProposedPerson = proposedPerson,
                            AdditionalNote = request.AdditionalNote,

                            ProposedChanges = request.ProposedChanges.SerializeToString(),

                            OrgPositionId = request.OrgPositionId,
                            OrgPositionInstanceId = request.OrgPositionInstanceId,

                            IsDraft = request.IsDraft,

                            Created = created,
                            CreatedBy = request.Editor.Person,
                            LastActivity = created,

                            ProposedPersonWasNotified = false, // Should be set/reset during update when/if when notifications are enabled

                        };

                        await db.ResourceAllocationRequests.AddAsync(item);
                        await db.SaveChangesAsync();

                        var workflow = new InternalRequestJointVentureWorkflowV1(request.Editor.Person);
                        await db.Workflows.AddAsync(workflow.CreateDatabaseEntity(item.Id, DbRequestType.Employee));
                        await db.SaveChangesAsync();


                        return item;
                    }

                    private static DbResourceAllocationRequest.DbAllocationRequestType ParseRequestType(Create request)
                    {
                        return Enum.Parse<DbResourceAllocationRequest.DbAllocationRequestType>($"{request.Type}");
                    }

                  
                    private async Task ValidateOrgPositionAsync(Create request)
                    {
                        var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId!.Value);

                        if (position is null)
                            throw InvalidOrgChartPositionError.NotFound(request.OrgPositionId.Value);

                        if (position.Project.ProjectId != request.OrgProjectId)
                            throw InvalidOrgChartPositionError.InvalidProject(position);
                    }

                    private async Task<DbProject?> EnsureProjectAsync(Create request)
                    {
                        var orgProject = await orgResolver.ResolveProjectAsync(request.OrgProjectId);
                        if (orgProject == null)
                            return null;

                        var project =
                            await db.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId) ??
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
