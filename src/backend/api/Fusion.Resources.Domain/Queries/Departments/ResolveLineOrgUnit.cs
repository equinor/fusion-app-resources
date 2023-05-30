using Fusion.Integration.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class ResolveLineOrgUnit : IRequest<ApiOrgUnit?>
    {
        public string DepartmentId { get; }

        public ResolveLineOrgUnit(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public class Handler : IRequestHandler<ResolveLineOrgUnit, ApiOrgUnit?>
        {
            private readonly ILineOrgResolver lineOrgResolver;

            public Handler(ILineOrgResolver lineOrgResolver)
            {
                this.lineOrgResolver = lineOrgResolver;
            }

            public async Task<ApiOrgUnit?> Handle(ResolveLineOrgUnit request, CancellationToken cancellationToken)
            {
                var lineOrgDpt = await lineOrgResolver.ResolveOrgUnitAsync(Integration.LineOrg.DepartmentId.FromFullPath(request.DepartmentId));

                return lineOrgDpt;

            }
        }
    }
}