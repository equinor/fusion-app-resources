using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// This needs to be refactored into an injectable service, that cache the whole responsibility matrix. 
    /// The number of items here should not be that great...
    /// </summary>
    public class RequestRouter
    {
        private const int min_score = 7;
        private readonly ResourcesDbContext db;
        private readonly IProjectOrgResolver orgResolver;
        private readonly IMediator mediator;

        public RequestRouter(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator)
        {
            this.db = db;
            this.orgResolver = orgResolver;
            this.mediator = mediator;
        }

        public async Task<string?> RouteAsync(DbResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            var departmentId = await RouteFromResponsibilityMatrix(request, cancellationToken);

            if (string.IsNullOrEmpty(departmentId))
            {
                departmentId = await RouteFromBasePosition(request);
            }

            return departmentId;
        }

        private async Task<string?> RouteFromBasePosition(DbResourceAllocationRequest request)
        {
            if (!request.OrgPositionId.HasValue) return null;

            var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
            var departmentPath = position?.BasePosition?.Department;
            if(!string.IsNullOrEmpty(departmentPath))
            {
                // Check if department path is an actual department
                // TODO: Maybe round robin when partial match?

                var actualDepartment = await mediator.Send(new GetDepartment(departmentPath));
                departmentPath = actualDepartment?.DepartmentId;
            }

            return departmentPath;
        }

        private async Task<string?> RouteFromResponsibilityMatrix(DbResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            var props = new MatchingProperties(request.Project.OrgProjectId)
            {
                Discipline = request.Discipline,
                LocationId = request.OrgPositionInstance.LocationId,
            };
            var matches = Match(props);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= min_score, cancellationToken);

            return bestMatch?.Row.Unit;
        }

        private IQueryable<ResponsibilityMatch> Match(MatchingProperties props)
        {
            return db.ResponsibilityMatrices
                .Include(m => m.Responsible)
                .Include(m => m.Project)
                .Select(m => new ResponsibilityMatch
                {
                    Score = (m.Project!.OrgProjectId == props.OrgProjectId ? 7 : 0)
                            + (props.BasePositionDepartment != null && m.Unit!.StartsWith(props.BasePositionDepartment) ? 5 : 0)
                            + (m.Discipline == props.Discipline ? 2 : 0)
                            + (m.LocationId == props.LocationId ? 1 : 0),
                    Row = m
                })
                .OrderByDescending(x => x.Score);
        }

        private class MatchingProperties
        {
            public MatchingProperties(Guid orgProjectId)
            {
                OrgProjectId = orgProjectId;
            }
            public Guid OrgProjectId { get; }
            public string? Discipline { get; set; }
            public string? BasePositionDepartment { get; set; }
            public Guid? LocationId { get; set; }
        }

        public class ResponsibilityMatch
        {
            public int Score { get; set; }
            public DbResponsibilityMatrix Row { get; set; } = null!;
        }
    }

}
