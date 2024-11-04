using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands.BaseHandlers;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Fusion.Resources.Domain.Commands
{

    public class CreateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public Guid? ProjectId { get; set; }
        public Guid? LocationId { get; set; }
        public string? Discipline { get; set; }
        public Guid? BasePositionId { get; set; }
        public string? Sector { get; set; }
        public string Unit { get; set; } = null!;
        public Guid? ResponsibleId { get; set; }

        public class Handler : ResponsibilityMatrixBaseHandler, IRequestHandler<CreateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService, IProjectOrgResolver orgResolver, IMediator mediator)
                : base(resourcesDb, profileService, orgResolver, mediator)
            {
            }

            public async Task<QueryResponsibilityMatrix> Handle(CreateResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                DbProject? project = null;
                if (request.ProjectId != null)
                {
                    project = await EnsureProjectAsync(request.ProjectId.Value);
                    if (project == null)
                        throw new ArgumentException("Unable to resolve project using org service");
                }

                DbPerson? responsible = await GetResourceOwner(request.Unit);

                var newItem = new DbResponsibilityMatrix
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = request.Editor.Person,
                    Created = DateTimeOffset.UtcNow,
                    Project = project,
                    LocationId = request.LocationId,
                    Discipline = request.Discipline,
                    BasePositionId = request.BasePositionId,
                    Sector = request.Sector,
                    Unit = request.Unit,
                    Responsible = responsible
                };

                resourcesDb.ResponsibilityMatrices.Add(newItem);
                await resourcesDb.SaveChangesAsync(cancellationToken);

                return new QueryResponsibilityMatrix(newItem);
            }
        }
    }
}
