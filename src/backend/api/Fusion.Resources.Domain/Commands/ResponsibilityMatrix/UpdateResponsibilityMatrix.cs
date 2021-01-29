using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain.Commands
{
    public class UpdateResponsibilityMatrix : TrackableRequest<QueryResponsibilityMatrix>
    {
        public UpdateResponsibilityMatrix(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; } = null!;
        public QueryProject Project { get; set; } = null!;
        public QueryLocation Location { get; set; }
        public string? Discipline { get; set; }
        public QueryBasePosition BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public QueryPerson Responsible { get; set; } = null!;

        public class Handler : IRequestHandler<UpdateResponsibilityMatrix, QueryResponsibilityMatrix>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
            }

            public async Task<QueryResponsibilityMatrix> Handle(UpdateResponsibilityMatrix request, CancellationToken cancellationToken)
            {
                var status = await resourcesDb.ResponsibilityMatrices
                    .Include(cp => cp.CreatedBy)
                    .Include(cp => cp.Project)
                    .Include(cp => cp.Responsible)
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (status is null)
                    throw new ArgumentException($"Cannot locate status using identifier '{request.Id}'");

/*                status.Created = request.Created;
                status.CreatedBy = request.CreatedBy;
                status.Project = new QueryProject(matrix.Project);
                status.Location = new QueryLocation(matrix.LocationId);
                status.Discipline = matrix.Discipline;
                status.BasePosition = new QueryBasePosition(matrix.BasePositionId);
                status.Sector = matrix.Sector;
                status.Unit = matrix.Unit;
                status.Responsible = new QueryPerson(matrix.Responsible);*/
                


                await resourcesDb.SaveChangesAsync();

                var returnItem = await mediator.Send(new GetResponsibilityMatrixItem(request.Id));
                return returnItem;
            }
        }
    }
}
