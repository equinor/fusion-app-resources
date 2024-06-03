using Fusion.Integration;
using Fusion.Resources.Database;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartment : IRequest<QueryDepartment?>
    {
        private bool shouldExpandDelegatedResourceOwners;

        public string DepartmentId { get; }

        public GetDepartment(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public GetDepartment ExpandDelegatedResourceOwners()
        {
            shouldExpandDelegatedResourceOwners = true;
            return this;
        }

        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartment, QueryDepartment?>
        {
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext  db, IFusionProfileResolver profileResolver, IMediator mediator)
                : base(db, profileResolver)
            {
                this.mediator = mediator;
            }

            public async Task<QueryDepartment?> Handle(GetDepartment request, CancellationToken cancellationToken)
            {

                var orgUnit = await mediator.Send(new ResolveLineOrgUnit(request.DepartmentId));

                if (orgUnit is null) return null;

                var sector = new DepartmentPath(orgUnit.FullDepartment).Parent();
                var result = new QueryDepartment(orgUnit)
                {
                    SectorId = sector
                };

                if (request.shouldExpandDelegatedResourceOwners)
                    await ExpandDelegatedResourceOwner(result, cancellationToken);

                return result;
            }
        }
    }
}