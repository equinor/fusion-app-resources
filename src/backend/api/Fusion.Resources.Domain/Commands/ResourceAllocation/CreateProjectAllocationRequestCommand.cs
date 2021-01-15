using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateProjectAllocationRequestCommand : TrackableRequest<QueryResourceAllocationRequest>
    {
        public CreateProjectAllocationRequestCommand(Guid createdById)
        {
            this.CreatedById = createdById;
        }

        public Guid CreatedById { get; set; }
        public string? Discipline { get; set; }
        public QueryResourceAllocationRequest.QueryAllocationRequestType Type { get; set; }
        public QueryWorkflow? Workflow { get; set; }
        public DbRequestState State { get; set; }

        public Guid OrgProjectId { get; set; }
        public Guid? OrgPositionId { get; set; }
        public QueryResourceAllocationRequestOrgPositionInstance OrgPositionInstance { get; set; }

        public Guid ProposedPersonId { get; set; }
        public string? AdditionalNote { get; set; }

        public IEnumerable<QueryProposedChange>? ProposedChanges { get; set; }

        public bool IsDraft { get; set; }
        public QueryProvisioningStatus ProvisioningStatus { get; set; }

        public class Handler : IRequestHandler<CreateProjectAllocationRequestCommand, QueryResourceAllocationRequest>
        {
            private readonly IProfileService profileService;
            private readonly IOrgApiClient orgClient;
            private readonly ResourcesDbContext db;
            public Handler(IProfileService profileService, IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext db)
            {
                this.profileService = profileService;
                this.orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.db = db;
            }

            public async Task<QueryResourceAllocationRequest> Handle(CreateProjectAllocationRequestCommand request, CancellationToken cancellationToken)
            {
                var createdByPerson = await profileService.EnsurePersonAsync(request.CreatedById);
                var proposedPerson = await profileService.EnsurePersonAsync(request.ProposedPersonId);

                if (createdByPerson == null || proposedPerson == null)
                {
                    throw new ProfileNotFoundError("ProfileNotFound", null);
                }

                var project = await EnsureProject(request);
                if (project == null)
                {
                    //TODO: Make this better...
                    throw new Exception("Project not found");
                }

                var item = new DbResourceAllocationRequest
                {
                    Discipline = request.Discipline,
                    Type = Enum.Parse<DbResourceAllocationRequest.DbAllocationRequestType>($"{request.Type}"),
                    State = request.State,

                    Project = project,

                    ProposedPersonId = proposedPerson.Id,
                    AdditionalNote = request.AdditionalNote,

                    Created = DateTimeOffset.UtcNow,
                    CreatedById = createdByPerson.Id,

                    LastActivity = DateTimeOffset.UtcNow,
                    IsDraft = request.IsDraft

                    //Workflow = workflow,
                    //OrgPositionId = entity.OriginalPositionId,
                    //OrgPositionInstance = new QueryResourceAllocationRequestOrgPositionInstance(entity.OrgPositionInstance),
                    //if (entity.ProposedChanges != null)ProposedChanges = JsonConvert.DeserializeObject<IEnumerable<QueryProposedChange>>(entity.ProposedChanges),

                };
                await db.ResourceAllocationRequests.AddAsync(item);
                await db.SaveChangesAsync();

                return new QueryResourceAllocationRequest(item);
            }

            private async Task<DbProject?> EnsureProject(CreateProjectAllocationRequestCommand request)
            {
                var orgProject = await orgClient.GetProjectOrDefaultV2Async(request.OrgProjectId);
                if (orgProject == null)
                    return null;
                var project = await db.Projects.FirstOrDefaultAsync(x => x.OrgProjectId == request.OrgProjectId);
                if (project == null)
                {
                    project = new DbProject
                    {
                        Name = orgProject.Name,
                        OrgProjectId = orgProject.ProjectId,
                        DomainId = orgProject.DomainId
                    };
                }

                return project;
            }
        }

    }
}
