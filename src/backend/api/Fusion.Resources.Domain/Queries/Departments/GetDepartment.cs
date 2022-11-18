using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using static Fusion.Integration.LineOrg.DepartmentId;

namespace Fusion.Resources.Domain
{
    public class GetDepartment : IRequest<QueryDepartment?>
    {

        private bool shouldExpandDelegatedResourceOwners;
        private bool shouldIncludeName;
       

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
        public GetDepartment IncludeName()
        {
            shouldIncludeName = true;
            return this;
        }
      

        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartment, QueryDepartment?>
        {
            public Handler(IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
                : base(rolesClient, lineOrgResolver, profileResolver) { }

            public async Task<QueryDepartment?> Handle(GetDepartment request, CancellationToken cancellationToken)
            {
                var lineOrgDpt = await lineOrgResolver.ResolveDepartmentAsync(Integration.LineOrg.DepartmentId.FromFullPath(request.DepartmentId));

                if (lineOrgDpt is null)
                    lineOrgDpt = await lineOrgResolver.ResolveDepartmentAsync(Integration.LineOrg.DepartmentId.FromSapId(request.DepartmentId));
                
           


                QueryDepartment? result;
                if (lineOrgDpt is null) return null;

                var sector = new DepartmentPath(lineOrgDpt.FullName).Parent();
                if (request.shouldIncludeName)
                {
                    result = new QueryDepartment(lineOrgDpt.FullName, sector, lineOrgDpt.Name);

                }
                else
                {
                    result = new QueryDepartment(lineOrgDpt.FullName, sector);
                }




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