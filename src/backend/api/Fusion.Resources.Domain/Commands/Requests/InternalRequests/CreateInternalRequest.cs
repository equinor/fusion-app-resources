using FluentValidation;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateInternalRequest : TrackableRequest<QueryResourceAllocationRequest>
    {
        public CreateInternalRequest(InternalRequestOwner owner, InternalRequestType type)
        {
            Owner = owner;
            Type = type;
            IsDraft = true;
        }

        public Guid OrgProjectId { get; set; }
        public string? AssignedDepartment { get; set; }
        public Guid? ProposedPersonAzureUniqueId { get; set; }

        public InternalRequestOwner Owner { get; set; }
        public InternalRequestType Type { get; set; }
        public string? SubType { get; set; }

        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }
        public string? AdditionalNote { get; set; }
        public Dictionary<string, object>? ProposedChanges { get; set; }
        public bool IsDraft { get; set; }
        public Guid? CorrelationId { get; set; }

        public class Validator : AbstractValidator<CreateInternalRequest>
        {
            public Validator(ResourcesDbContext db)
            {

                RuleFor(x => x.OrgPositionInstanceId)
                    .MustAsync(async (id, cancel) =>
                    {
                        return !await db.ResourceAllocationRequests.AnyAsync(r => r.OrgPositionInstance.Id == id && !r.State.IsCompleted);
                    })
                    .WithMessage("Cannot create multiple requests on same instance.");

                RuleFor(x => x.OrgProjectId).NotEmpty();
                RuleFor(x => x.OrgPositionId).NotEmpty();
                RuleFor(x => x.OrgPositionInstanceId).NotEmpty();
            }
        }



        public class Handler : IRequestHandler<CreateInternalRequest, QueryResourceAllocationRequest>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IProjectOrgResolver orgResolver, IMediator mediator, IProfileService profileService)
            {
                this.dbContext = dbContext;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
                this.profileService = profileService;
            }

            public async Task<QueryResourceAllocationRequest> Handle(CreateInternalRequest request, CancellationToken cancellationToken)
            {
                var dbItem = await CreateDbRequestAsync(request);

                await dbContext.SaveChangesAsync(cancellationToken);

                await mediator.Publish(new Notifications.InternalRequests.InternalRequestCreated(dbItem.Id));

                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(dbItem.Id), cancellationToken);
                return requestItem!;
            }

            private async Task<DbResourceAllocationRequest> CreateDbRequestAsync(CreateInternalRequest request)
            {
                var created = DateTimeOffset.UtcNow;

                var resolvedProject = await EnsureProjectAsync(request);
                var position = await ResolveOrgPositionAsync(request);
                var proposedPerson = await ResolveProposedPersonAsync(request);


                var instance = position.Instances.FirstOrDefault(i => i.Id == request.OrgPositionInstanceId);
                if (instance is null)
                    throw new InvalidOperationException($"Could not locate instance with id {request.OrgPositionInstanceId} on position {request.OrgPositionId}");

                var item = new DbResourceAllocationRequest
                {
                    Id = Guid.NewGuid(),
                    AssignedDepartment = request.AssignedDepartment,    // Will be overwritten later
                    Type = request.Type.MapToDatabase(),
                    SubType = request.SubType,
                    RequestOwner = request.Owner switch
                    {
                        InternalRequestOwner.Project => DbInternalRequestOwner.Project,
                        InternalRequestOwner.ResourceOwner => DbInternalRequestOwner.ResourceOwner,
                        _ => throw new NotSupportedException("Owner type not supported by db")
                    },

                    Project = resolvedProject,
                    Discipline = position.BasePosition.Discipline,

                    AdditionalNote = request.AdditionalNote,

                    OrgPositionId = request.OrgPositionId,
                    OrgPositionInstance = new DbResourceAllocationRequest.DbOpPositionInstance
                    {
                        Id = request.OrgPositionInstanceId,
                        AppliesFrom = instance.AppliesFrom,
                        AppliesTo = instance.AppliesTo,
                        AssignedToMail = instance.AssignedPerson?.Mail,
                        AssignedToUniqueId = instance.AssignedPerson?.AzureUniqueId,
                        LocationId = instance.Location?.Id,
                        Obs = instance.Obs,
                        Workload = instance.Workload
                    },
                    InitialProposedPerson = proposedPerson is not null
                        ? new DbResourceAllocationRequest.DbOpInitialProposedPerson()
                        {
                            AzureUniqueId = proposedPerson.AzureUniqueId,
                            Mail = proposedPerson.Mail
                        }
                        : null,

                    IsDraft = request.IsDraft,

                    Created = created,
                    CreatedBy = request.Editor.Person,
                    LastActivity = created,
                    CorrelationId = request.CorrelationId
                };

                await UpdateAssignedOrgUnitAsync(request, item);

                if (proposedPerson is not null)
                {
                    item.ProposePerson(proposedPerson);
                }

                dbContext.ResourceAllocationRequests.Add(item);


                return item;
            }

            private async Task UpdateAssignedOrgUnitAsync(CreateInternalRequest request, DbResourceAllocationRequest dbItem)
            {
                if (!string.IsNullOrEmpty(request.AssignedDepartment))
                {
                    var orgUnit = await mediator.Send(new ResolveLineOrgUnit(request.AssignedDepartment));

                    // If the assigned department is provided as input, it should be validated that it exists.
                    if (orgUnit is null)
                    {
                        throw new InvalidOperationException($"Could not resolve org unit using identifier '{request.AssignedDepartment}'. Unable to set assigned department");
                    }

                    dbItem.AssignedDepartment = orgUnit.FullDepartment;
                    dbItem.AssignedDepartmentId = orgUnit.SapId;
                }
            }

            private async Task<DbPerson?> ResolveProposedPersonAsync(CreateInternalRequest request)
            {
                if (request.ProposedPersonAzureUniqueId.HasValue)
                {
                    var personId = (PersonId)request.ProposedPersonAzureUniqueId;
                    return await profileService.EnsurePersonAsync(personId);
                }
                return null;
            }

            private async Task<ApiPositionV2> ResolveOrgPositionAsync(CreateInternalRequest request)
            {
                var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId);

                if (position is null)
                    throw InvalidOrgChartPositionError.NotFound(request.OrgPositionId);

                if (position.Project.ProjectId != request.OrgProjectId)
                    throw InvalidOrgChartPositionError.InvalidProject(position);

                return position;
            }

            private async Task<DbProject> EnsureProjectAsync(CreateInternalRequest request)
            {
                var orgProject = await orgResolver.ResolveProjectAsync(request.OrgProjectId);
                if (orgProject == null)
                    throw new InvalidOperationException("Project does not exist in org chart service");

                var project = await dbContext.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId) ??
                    new DbProject
                    {
                        Name = orgProject.Name,
                        OrgProjectId = orgProject.ProjectId,
                        DomainId = orgProject.DomainId,
                        State = orgProject.State.ResolveProjectState(),
                    };

                return project;
            }
        }
    }
}
