using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Application.LineOrg;
using Fusion.Resources.Application.LineOrg.Models;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if (!string.IsNullOrEmpty(sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == sector);
            }

            if (!string.IsNullOrEmpty(departmentIdStartsWith))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(departmentIdStartsWith));
            }

            if (departmentIds?.Any() == true)
            {
                departments = departments.Where(dpt => departmentIds.Contains(dpt.DepartmentId));
            }

            return departments.Select(dpt => new QueryDepartment(dpt));
        }

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

                var trackedDepartments = await request.Execute(db.Departments).ToListAsync(cancellationToken);
                var lineOrgDepartments = await lineOrgResolver.GetResourceOwners(request.resourceOwnerSearch, cancellationToken);

                if (request.departmentIds is not null)
                {
                    var ids = new HashSet<string>(request.departmentIds);
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => ids.Contains(x.DepartmentId))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.sector))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.DepartmentId).Parent() == request.sector)
                        .ToList();
                }

                if (!string.IsNullOrEmpty(request.departmentIdStartsWith))
                {
                    lineOrgDepartments = lineOrgDepartments
                        .Where(x => new DepartmentPath(x.DepartmentId).Parent() == request.sector)
                        .ToList();
                }

                result = MergeResults(trackedDepartments, lineOrgDepartments);

                // Cannot filter requests from db before merging with line org results as we need to
                // 1. Maintain sector info if tracked in db, and 
                // 2. Search info from line org if it exists there.
                if (!string.IsNullOrEmpty(request.resourceOwnerSearch))
                {
                    result = result.Where(dpt =>
                        dpt.DepartmentId.Contains(request.resourceOwnerSearch)
                        || dpt.LineOrgResponsible?.Name.Contains(request.resourceOwnerSearch) == true
                        || dpt.LineOrgResponsible?.Mail?.Contains(request.resourceOwnerSearch) == true
                    ).ToList();
                }

                if (request.shouldExpandDelegatedResourceOwners)
                {
                    await ExpandDelegatedResourceOwner(result, cancellationToken);
                }

                return result;
            }

            private static List<QueryDepartment> MergeResults(List<QueryDepartment> trackedDepartments, List<LineOrgDepartment> lineOrgDepartments)
            {
                var departmentMap = trackedDepartments.ToDictionary(dpt => dpt.DepartmentId);
                foreach (var lineOrgDepartment in lineOrgDepartments)
                {
                    if (departmentMap.ContainsKey(lineOrgDepartment.DepartmentId))
                    {
                        departmentMap[lineOrgDepartment.DepartmentId].LineOrgResponsible = lineOrgDepartment.Responsible;
                    }
                    else
                    {
                        trackedDepartments.Add(new QueryDepartment(lineOrgDepartment));
                    }
                }
                return trackedDepartments;
            }
        }
    }
}
