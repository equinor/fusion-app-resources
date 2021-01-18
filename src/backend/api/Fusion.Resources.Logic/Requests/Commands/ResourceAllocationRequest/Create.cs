using System;
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
        public class Create : TrackableRequest<QueryResourceAllocationRequest>
        {
            public Create(Guid orgProjectId)
            {
                OrgProjectId = orgProjectId;
            }

            private string? Discipline { get; set; }
            private QueryResourceAllocationRequest.QueryAllocationRequestType Type { get; set; }

            private Guid OrgProjectId { get; }
            private Guid? OrgPositionId { get; set; }

            private Domain.ResourceAllocationRequest.QueryPositionInstance OrgPositionInstance { get; } =
                new Domain.ResourceAllocationRequest.QueryPositionInstance();

            private Guid ProposedPersonId { get; set; }
            private string? AdditionalNote { get; set; }

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

            public Create WithProposedPerson(Guid proposedPersonId)
            {
                ProposedPersonId = proposedPersonId;
                return this;
            }

            public Create WithAdditionalNode(string? note)
            {
                AdditionalNote = note;
                return this;
            }

            public Create WithPosition(Guid basePositionId, DateTime from, DateTime to, double workload, string? obs,
                string? location)
            {
                OrgPositionInstance.Id = basePositionId;
                OrgPositionInstance.Workload = workload;
                OrgPositionInstance.AppliesFrom = from;
                OrgPositionInstance.AppliesTo = to;
                OrgPositionInstance.Obs = obs ?? string.Empty;
                OrgPositionInstance.Location = location ?? string.Empty;

                return this;
            }

            public class Handler : IRequestHandler<Create, QueryResourceAllocationRequest
            >
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

                private DbProject Project { get; set; }
                private DbPerson ProposedPerson { get; set; }

                public async Task<QueryResourceAllocationRequest> Handle(Create request, CancellationToken cancellationToken)
                {
                    // Validate references.
                    await ValidateAsync(request);

                    var item = await PersistChangesAsync(request);

                    // Start the workflow
                    //await mediator.Send(new Initialize(item.Id));


                    var dbRequest = await mediator.Send(new GetProjectResourceAllocationRequestItem(item.Id));
                    return dbRequest;
                }

                private async Task<DbResourceAllocationRequest> PersistChangesAsync(Create request)
                {
                    var created = DateTimeOffset.UtcNow;

                    var item = new DbResourceAllocationRequest
                    {
                        Discipline = request.Discipline,
                        Type = Enum.Parse<DbResourceAllocationRequest.DbAllocationRequestType>($"{request.Type}"),
                        State = DbRequestState.Created,

                        Project = Project,

                        ProposedPersonId = ProposedPerson.Id,
                        AdditionalNote = request.AdditionalNote,

                        Created = created,
                        CreatedBy = request.Editor.Person,

                        LastActivity = created,

                        OriginalPositionId = request.OrgPositionId,
                        ResourceAllocationOrgPositionInstance = GenerateOrgPositionInstance(request.OrgPositionInstance)
                    };
                    
                    await db.ResourceAllocationRequests.AddAsync(item);
                    await db.SaveChangesAsync();

                    return item;
                }

                private async Task ValidateAsync(Create request)
                {
                    var proposed = await profileService.EnsurePersonAsync(request.ProposedPersonId);

                    if (proposed is null) throw new ProfileNotFoundError("ProfileNotFound", null);

                    ProposedPerson = proposed;

                    /*var project = await db.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == request.OrgProjectId);
                    if (project is null) throw new InvalidOperationException("Could not locate the project!");
                    */

                    var project = await EnsureProjectAsync(request);
                    if (project is null)
                    {
                        throw new InvalidOperationException("Could not locate the project!");
                    }
                    Project = project;

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