using Fusion.Integration;
using Fusion.Resources.Application.LineOrg;
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
            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
                : base(db, lineOrgResolver, profileResolver) { }

            public async Task<QueryDepartment?> Handle(GetDepartment request, CancellationToken cancellationToken)
            {
                var trackedDepartment = await db.Departments.FindAsync(new[] { request.DepartmentId }, cancellationToken);
                var lineOrgDpt = await lineOrgResolver.GetDepartment(request.DepartmentId);

                QueryDepartment? result;
                if (trackedDepartment is not null)
                    result = new QueryDepartment(trackedDepartment);
                else if (lineOrgDpt is not null)
                    result = new QueryDepartment(lineOrgDpt.DepartmentId, null);
                else
                    return null;

                if (request.shouldExpandDelegatedResourceOwners)
                    await ExpandDelegatedResourceOwner(result, cancellationToken);

                result.LineOrgResponsible = lineOrgDpt?.Responsible;

                return result;
            }
        }
    }
}
