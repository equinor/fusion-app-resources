using FluentValidation;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateInternalRequest : TrackableRequest<QueryResourceAllocationRequest>
    {
        public CreateInternalRequest(InternalRequestType type, bool isDraft)
        {
            Type = type;
            IsDraft = isDraft;
        }

        public Guid OrgProjectId { get; set; }
        public string? AssignedDepartment { get; set; }

        public InternalRequestType Type { get; set; }

        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }
        public string? AdditionalNote { get; set; }
        public Dictionary<string, object>? ProposedChanges { get; set; }
        public bool IsDraft { get; set; }


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
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IProjectOrgResolver orgResolver, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
            }

            public async Task<QueryResourceAllocationRequest> Handle(CreateInternalRequest request, CancellationToken cancellationToken)
            {
                var dbItem = await CreateDbRequestAsync(request);

                await dbContext.SaveChangesAsync(cancellationToken);

                var requestItem = await mediator.Send(new GetResourceAllocationRequestItem(dbItem.Id), cancellationToken);
                return requestItem!;
            }

            private async Task<DbResourceAllocationRequest> CreateDbRequestAsync(CreateInternalRequest request)
            {
                var created = DateTimeOffset.UtcNow;

                var resolvedProject = await EnsureProjectAsync(request);
                var position = await ResolveOrgPositionAsync(request);

                var instance = position.Instances.FirstOrDefault(i => i.Id == request.OrgPositionInstanceId);
                if (instance is null)
                    throw new InvalidOperationException($"Could not locate instance with id {request.OrgPositionInstanceId} on position {request.OrgPositionId}");


                var item = new DbResourceAllocationRequest
                {
                    Id = Guid.NewGuid(),
                    AssignedDepartment = request.AssignedDepartment,
                    Type = request.Type switch {
                        InternalRequestType.Normal => DbInternalRequestType.Normal,
                        InternalRequestType.Direct => DbInternalRequestType.Direct,
                        InternalRequestType.JointVenture => DbInternalRequestType.JointVenture,
                        _ => throw new NotSupportedException("Query request type ")
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

                    IsDraft = request.IsDraft,

                    Created = created,
                    CreatedBy = request.Editor.Person,
                    LastActivity = created

                };

                dbContext.ResourceAllocationRequests.Add(item);

                return item;
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
                        DomainId = orgProject.DomainId
                    };

                return project;
            }
        }
    }
}
