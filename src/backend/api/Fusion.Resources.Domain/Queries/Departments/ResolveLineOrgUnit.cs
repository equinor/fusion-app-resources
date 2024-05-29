using Fusion.Integration.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class ResolveLineOrgUnit : IRequest<ApiOrgUnit?>
    {
        public string DepartmentId { get; }

        /// <summary>
        /// Resolve org unit from line org from either full department string or sap id.
        /// </summary>
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
                if (string.IsNullOrEmpty(request.DepartmentId))
                    return null;

                var departmentId = Regex.IsMatch(request.DepartmentId, @"\d+") ? Integration.LineOrg.DepartmentId.FromSapId(request.DepartmentId)
                    : Integration.LineOrg.DepartmentId.FromFullPath(request.DepartmentId);

                var lineOrgDpt = await lineOrgResolver.ResolveOrgUnitAsync(departmentId);

                return lineOrgDpt;

            }
        }
    }
}