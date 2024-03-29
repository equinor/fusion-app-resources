using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Application;
using Fusion.Resources.Database;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartments : IRequest<IEnumerable<QueryDepartment>>
    {
        private bool shouldExpandDelegatedResourceOwners = false;
        private string? resourceOwnerSearch;

        private string? departmentIdStartsWith;
        private string? sector;
        private string[]? departmentIds = null;

        public GetDepartments StartsWith(string department)
        {
            this.departmentIdStartsWith = department;
            return this;
        }

        public GetDepartments ByIds(params string[] departmentIds)
        {
            this.departmentIds = departmentIds;
            return this;
        }

        public GetDepartments ByIds(IEnumerable<string> departmentIds)
        {
            this.departmentIds = departmentIds.ToArray();
            return this;
        }

        public GetDepartments InSector(string sector)
        {
            this.sector = sector;
            return this;
        }

        public GetDepartments ExpandDelegatedResourceOwners()
        {
            shouldExpandDelegatedResourceOwners = true;
            return this;
        }

        public GetDepartments WhereResourceOwnerMatches(string search)
        {
            resourceOwnerSearch = search;
            return this;
        }

        public class Handler : DepartmentHandlerBase, IRequestHandler<GetDepartments, IEnumerable<QueryDepartment>>
        {
            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
                : base(db, lineOrgResolver, profileResolver) { }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                List<QueryDepartment> result;

                IEnumerable<ApiLineOrgUser> lineOrgDepartments;
                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                    lineOrgDepartments = await lineOrgResolver.ResolveResourceOwnersAsync(request.resourceOwnerSearch);
                else
                    lineOrgDepartments = await lineOrgResolver.ResolveResourceOwnersAsync();

                if (request.departmentIds is not null)
                {
                    var ids = new HashSet<string>(request.departmentIds);
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => ids.Contains(x.FullDepartment!))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.sector))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.FullDepartment!).Parent() == request.sector)
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.departmentIdStartsWith))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.FullDepartment!).Parent() == request.sector)
                        .ToList();
                }

                result = await lineOrgDepartments.ToQueryDepartment(profileResolver);

                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                {
                    result = result.Where(dpt =>
                        dpt.DepartmentId.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase)
                        || dpt.LineOrgResponsible?.Name.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase) == true
                        || dpt.LineOrgResponsible?.Mail?.Contains(request.resourceOwnerSearch, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (request.shouldExpandDelegatedResourceOwners)
                {
                    await ExpandDelegatedResourceOwner(result, cancellationToken);
                }

                return result;
            }
        }
    }
}