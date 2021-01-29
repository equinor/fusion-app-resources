using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable 

namespace Fusion.Resources.Domain.Commands
{

    public class CreateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public string? Discipline { get; set; }
        public class Handler : IRequestHandler<CreateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProfileService profileService;

            public Handler(ResourcesDbContext resourcesDb, IProfileService profileService)
            {
                this.resourcesDb = resourcesDb;
                this.profileService = profileService;
            }

            public async Task<QueryResponsibilityMatrix> Handle(CreateResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                /*var profile = await profileService.EnsurePersonAsync(request.PersonId);
                if (profile == null)
                    throw new ArgumentException("Cannot create personnel without either a valid azure unique id or mail address");
                */
                var newItem = new DbResponsibilityMatrix
                {
                    Id = Guid.NewGuid(),
/*                    Created = request.Created
                    CreatedBy = new QueryPerson(matrix.CreatedBy),
                    Project = new QueryProject(matrix.Project),
                    LocationId = new QueryLocation(matrix.LocationId),
                    Discipline = matrix.Discipline,
                    BasePositionId = new QueryBasePosition(matrix.BasePositionId),
                    Sector = matrix.Sector,
                    Unit = matrix.Unit,
                    Responsible = new QueryPerson(matrix.Responsible)*/
                };

                await resourcesDb.ResponsibilityMatrices.AddAsync(newItem);
                await resourcesDb.SaveChangesAsync();

                return new QueryResponsibilityMatrix(newItem);
            }

        }
    }
}
