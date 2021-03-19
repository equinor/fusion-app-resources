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
            var matches = Match(request.Project.OrgProjectId, request.Discipline, request.OrgPositionInstance.LocationId);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= 5, cancellationToken);

            return bestMatch?.Row.Unit;
        }

        public async Task<string?> Route(ApiPositionV2 tbnPosition, ApiPositionInstanceV2 instance, CancellationToken cancellationToken)
        {
            var matches = Match(tbnPosition.ProjectId, tbnPosition.BasePosition.Discipline, instance.Location?.Id);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= 5, cancellationToken);

            return bestMatch.Row?.Unit;
        }

        private IQueryable<ResponsibilityMatch> Match(
            Guid orgProjectId, 
            string? discipline, 
            Guid? locationId)
        {
            return db.ResponsibilityMatrices
                .Include(m => m.Responsible)
                .Include(m => m.Project)
                .Select(m => new ResponsibilityMatch
                {
                    Score = (m.Project!.OrgProjectId == orgProjectId ? 5 : 0)
                            + (m.Discipline == discipline ? 2 : 0)
                            + (m.LocationId == locationId ? 1 : 0),
                    Row = m
                })
                .OrderByDescending(x => x.Score);
        }

        public class ResponsibilityMatch
        {
            public int Score { get; set; }
            public DbResponsibilityMatrix Row { get; set; } = null!;
        }
    }

}
