using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Application.LineOrg;
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
        private bool shouldExpandResourceOwners = false;
        private string? resourceOwnerSearch;

        private string? departmentFilter;
        private string? sector;
        private string[]? departmentIds = null;

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if (!string.IsNullOrEmpty(sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == sector);
            }

            if (!string.IsNullOrEmpty(departmentFilter))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(departmentFilter));
            }

            if (departmentIds?.Any() == true)
            {
                departments = departments.Where(dpt => departmentIds.Contains(dpt.DepartmentId));
            }

            return departments.Select(dpt => new QueryDepartment(dpt));
        }

        public GetDepartments StartsWith(string department)
        {
            this.departmentFilter = department;
            return this;
        }

        public GetDepartments ById(string departmentId)
        {
            departmentIds = new[] { departmentId };
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

        public GetDepartments ExpandResourceOwners()
        {
            shouldExpandResourceOwners = true;
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

        public class Handler : IRequestHandler<GetDepartments, IEnumerable<QueryDepartment>>
        {
            private readonly ResourcesDbContext db;
            private readonly ILineOrgResolver lineOrgResolver;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.lineOrgResolver = lineOrgResolver;
                this.profileResolver = profileResolver;
            }

            public async Task<IEnumerable<QueryDepartment>> Handle(GetDepartments request, CancellationToken cancellationToken)
            {
                var result = new List<QueryDepartment>();
                var departments = await request.Execute(db.Departments)
                    .ToDictionaryAsync(dpt => dpt.DepartmentId, cancellationToken);

                if (request.shouldExpandResourceOwners)
                {
                    var searchedDepartments = departments.Keys!.ToHashSet();
                    if (request.departmentIds?.Any() == true)
                    {
                        foreach (var departmentId in request.departmentIds)
                        {
                            if (!searchedDepartments.Contains(departmentId))
                                searchedDepartments.Add(departmentId);
                        }
                    }

                    // Optimize search when searching for a specific department
                    if (request.resourceOwnerSearch is null && request.departmentIds?.Length == 1)
                    {
                        request.resourceOwnerSearch = request.departmentIds.Single();
                    }

                    var resourceOwners = await lineOrgResolver
                        .GetResourceOwners(request.resourceOwnerSearch, cancellationToken);

                    foreach (var resourceOwner in resourceOwners)
                    {
                        if (request.departmentIds is not null && !searchedDepartments.Contains(resourceOwner.DepartmentId)) continue;
                        // Department found in line org but is not tracked in db
                        if (!departments.ContainsKey(resourceOwner.DepartmentId))
                        {
                            departments[resourceOwner.DepartmentId] = new QueryDepartment(resourceOwner.DepartmentId, null);
                        }

                        var department = departments[resourceOwner.DepartmentId];
                        department.LineOrgResponsible = resourceOwner.Responsible;

                        result.Add(department!);
                    }
                }
                else
                {
                    result = departments.Values.ToList();
                }

                if (request.shouldExpandDelegatedResourceOwners)
                {
                    foreach (var department in result)
                    {
                        var delegatedResourceOwners = await db.DepartmentResponsibles
                            .Where(r => r.DepartmentId == department.DepartmentId)
                            .ToListAsync(cancellationToken);

                        if (delegatedResourceOwners is not null)
                        {
                            var resolvedProfiles = await profileResolver
                                .ResolvePersonsAsync(delegatedResourceOwners.Select(p => new PersonIdentifier(p.ResponsibleAzureObjectId)));

                            department.DelegatedResourceOwners = resolvedProfiles
                                .Where(res => res.Success)
                                .Select(res => res.Profile!)
                                .ToList();
                        }
                    }

                }

                return result;
            }
        }
    }
}
