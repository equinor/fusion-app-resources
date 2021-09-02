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
    public class RequestRouter : IRequestRouter
    {
        private const int min_score = 7;
        private readonly ResourcesDbContext db;
        private readonly IProjectOrgResolver orgResolver;
        private readonly IMediator mediator;
        private readonly IProfileService profileService;

        public RequestRouter(ResourcesDbContext db, IProjectOrgResolver orgResolver, IMediator mediator, IProfileService profileService)
        {
            this.db = db;
            this.orgResolver = orgResolver;
            this.mediator = mediator;
            this.profileService = profileService;
        }

        public async Task<string?> RouteAsync(DbResourceAllocationRequest request, CancellationToken cancellationToken)
        {
            string? departmentId = null;

            if (request.ProposedPerson is not null)
            {
                departmentId = await RouteFromProposedPerson(request.ProposedPerson, cancellationToken);
            }

            if (string.IsNullOrEmpty(departmentId))
            {
                departmentId = await RouteFromBasePosition(request);
            }

            if (!string.IsNullOrEmpty(departmentId))
            {
                departmentId = await RouteFromResponsibilityMatrix(request, departmentId, cancellationToken);
            }

            return departmentId;
        }

        private async Task<string?> RouteFromProposedPerson(DbResourceAllocationRequest.DbOpProposedPerson proposedPerson, CancellationToken cancellationToken)
        {
            if (!proposedPerson.AzureUniqueId.HasValue) return null;

            var personId = new PersonId(proposedPerson.AzureUniqueId.Value);
            var profile = await profileService.ResolveProfileAsync(personId);

            return profile?.FullDepartment;
        }

        private async Task<string?> RouteFromBasePosition(DbResourceAllocationRequest request)
        {
            if (!request.OrgPositionId.HasValue) return null;

            var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
            var departmentPath = default(string);
            if (!string.IsNullOrEmpty(position?.BasePosition?.Department))
            {
                // Check if department path is an actual department
                var actualDepartment = await mediator.Send(new GetDepartment(position.BasePosition.Department));
                departmentPath = actualDepartment?.DepartmentId;
            }

            return departmentPath;
        }

        private async Task<string?> RouteFromResponsibilityMatrix(DbResourceAllocationRequest request, string departmentId, CancellationToken cancellationToken)
        {
            var props = new MatchingProperties(request.Project.OrgProjectId)
            {
                Discipline = request.Discipline,
                LocationId = request.OrgPositionInstance.LocationId,
                BasePositionDepartment = departmentId
            };
            var matches = Match(props);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= min_score, cancellationToken);

            return bestMatch?.Row.Unit ?? departmentId;
        }

        private IQueryable<ResponsibilityMatch> Match(MatchingProperties props)
        {
            return db.ResponsibilityMatrices
                .Include(m => m.Responsible)
                .Include(m => m.Project)
                .Select(m => new ResponsibilityMatch
                {
                    Score = (props.BasePositionDepartment != null && m.Unit!.StartsWith(props.BasePositionDepartment) ? 7 : 0)
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
