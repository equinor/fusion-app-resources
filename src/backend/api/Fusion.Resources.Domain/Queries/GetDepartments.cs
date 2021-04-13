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
        private string? departmentId;

        public IQueryable<QueryDepartment> Execute(IQueryable<DbDepartment> departments)
        {
            if(!string.IsNullOrEmpty(sector))
            {
                departments = departments.Where(dpt => dpt.SectorId == sector);
            }

            if(!string.IsNullOrEmpty(departmentFilter))
            {
                departments = departments.Where(dpt => dpt.DepartmentId.StartsWith(departmentFilter));
            }

            if (!string.IsNullOrEmpty(departmentId))
            {
                departments = departments.Where(dpt => dpt.DepartmentId == departmentId);
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
            this.departmentId = departmentId;
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

                if(request.shouldExpandResourceOwners)
                {
                    var searchedDepartments = departments.Keys!.ToHashSet();

                    var resourceOwners = await lineOrgResolver
                        .GetResourceOwners(request.resourceOwnerSearch, cancellationToken);

                    foreach(var resourceOwner in resourceOwners)
                    {
                        if (!searchedDepartments.Contains(resourceOwner.DepartmentId)) continue;

                        var department = departments[resourceOwner.DepartmentId];
                        department.LineOrgResponsible = resourceOwner.Responsible;
                        
                        result.Add(department!);
                    }
                }
                else
                {
                    result = departments.Values.ToList();
                }

                if(request.shouldExpandDelegatedResourceOwners)
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
