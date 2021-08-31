using Fusion.Integration;
using Fusion.Integration.LineOrg;
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
                var lineOrgDpt = await lineOrgResolver.ResolveDepartmentAsync(request.DepartmentId);

                QueryDepartment? result;
                if (lineOrgDpt is not null)
                {
                    var sector = new DepartmentPath(lineOrgDpt.FullName).Parent();
                    result = new QueryDepartment(lineOrgDpt.FullName, sector);
                }
                else
                    return null;

                if (request.shouldExpandDelegatedResourceOwners)
                    await ExpandDelegatedResourceOwner(result, cancellationToken);

                if (lineOrgDpt?.Manager?.AzureUniqueId is not null)
                {
                    result.LineOrgResponsible = await profileResolver.ResolvePersonBasicProfileAsync(new Integration.Profile.PersonIdentifier(lineOrgDpt.Manager.AzureUniqueId));
                }

                return result;
            }
        }
    }
}
