using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class RequestRouter
    {
        private readonly ResourcesDbContext db;

        public RequestRouter(ResourcesDbContext db)
        {
            this.db = db;
        }

        public async Task<string?> Route(DbResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            var props = new MatchingProperties(request.Project.OrgProjectId)
            {
                Discipline = request.Discipline,
                LocationId = request.OrgPositionInstance.LocationId,
            };
            var matches = Match(props);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= 5, cancellationToken);

            return bestMatch?.Row.Unit;
        }

        public async Task<string?> Route(ApiPositionV2 tbnPosition, ApiPositionInstanceV2 instance, CancellationToken cancellationToken)
        {
            var props = new MatchingProperties(tbnPosition.ProjectId)
            {
                BasePositionDepartment = tbnPosition.BasePosition.Department,
                Discipline = tbnPosition.BasePosition.Discipline,
                LocationId = instance.Location?.Id,
            };
            var matches = Match(props);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= 5, cancellationToken);

            return bestMatch.Row?.Unit;
        }

        private IQueryable<ResponsibilityMatch> Match(MatchingProperties props)
        {
            return db.ResponsibilityMatrices
                .Include(m => m.Responsible)
                .Include(m => m.Project)
                .Select(m => new ResponsibilityMatch
                {
                    Score = (props.BasePositionDepartment != null && m.Unit.StartsWith(props.BasePositionDepartment) ? 7 : 0)
                            + (m.Project!.OrgProjectId == props.OrgProjectId ? 5 : 0)
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
            public Guid OrgProjectId { get;  }
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
