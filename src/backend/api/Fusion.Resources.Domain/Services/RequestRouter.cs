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

            if (!String.IsNullOrEmpty(departmentId) || !request.OrgPositionId.HasValue) return departmentId;

            var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
            if (position is null) return departmentId;

            return await RouteAsync(position, request.OrgPositionInstance.Id, cancellationToken);
        }

        public async Task<string?> RouteAsync(ApiPositionV2 position, Guid? instanceId, CancellationToken cancellationToken)
        {
            var departmentId = await RouteFromBasePosition(position.BasePosition);
            departmentId = await RouteFromResponsibilityMatrix(position, instanceId, departmentId, cancellationToken);

            return departmentId;
        }


        private async Task<string?> RouteFromProposedPerson(DbResourceAllocationRequest.DbOpProposedPerson proposedPerson, CancellationToken cancellationToken)
        {
            if (!proposedPerson.AzureUniqueId.HasValue) return null;

            var personId = new PersonId(proposedPerson.AzureUniqueId.Value);
            var profile = await profileService.ResolveProfileAsync(personId);

            return profile?.FullDepartment;
        }

        private async Task<string?> RouteFromBasePosition(ApiPositionBasePositionV2 basePosition)
        {
            var departmentPath = default(string);
            if (!string.IsNullOrEmpty(basePosition.Department))
            {
                // Check if department path is an actual department
                var actualDepartment = await mediator.Send(new GetDepartment(basePosition.Department));
                departmentPath = actualDepartment?.DepartmentId;
            }

            return departmentPath;
        }

        private async Task<string?> RouteFromResponsibilityMatrix(ApiPositionV2 position, Guid? instanceId, string? departmentId, CancellationToken cancellationToken)
        {
            var instance = instanceId.HasValue
                ? position.Instances.FirstOrDefault(x => x.Id == instanceId)
                : position.Instances.FirstOrDefault();

            var props = new MatchingProperties(position.Project.ProjectId)
            {
                Discipline = position.BasePosition.Discipline,
                LocationId = instance?.Location?.Id,
                BasePositionDepartment = departmentId,
                BasePositionId = position.BasePosition.Id
            };
            var matches = Match(props);
            var bestMatch = await matches.FirstOrDefaultAsync(m => m.Score >= min_score && m.Score >= m.RequiredScore, cancellationToken);

            if (bestMatch?.Row.BasePositionId != null || IsRelevant(position, bestMatch?.Row.Unit))
            {
                return bestMatch!.Row.Unit;
            }

            return departmentId;
        }

        private static bool IsRelevant(ApiPositionV2 position, string? unit) => new DepartmentPath(position.BasePosition.Department).IsRelevant(unit);

        private IQueryable<ResponsibilityMatch> Match(MatchingProperties props)
        {
            return db.ResponsibilityMatrices
                .Include(m => m.Responsible)
                .Include(m => m.Project)
                .Select(m => new ResponsibilityMatch
                {
                    Score = (props.BasePositionDepartment != null && m.Unit!.StartsWith(props.BasePositionDepartment) ? 7 : 0)
                            + (m.Project!.OrgProjectId == props.OrgProjectId ? 5 : 0)
                            + (m.BasePositionId == props.BasePositionId ? 5 : 0)
                            + (m.Discipline == props.Discipline ? 2 : 0)
                            + (m.LocationId == props.LocationId ? 1 : 0),
                    RequiredScore = (m.Project != null ? 5 : 0)
                            + (m.BasePositionId != null ? 5 : 0)
                            + (m.Discipline != null ? 2 : 0)
                            + (m.LocationId != null ? 1 : 0),
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
            public Guid BasePositionId { get; internal set; }
        }

        public class ResponsibilityMatch
        {
            public int Score { get; set; }
            public DbResponsibilityMatrix Row { get; set; } = null!;
            public int RequiredScore { get; set; }
        }
    }
}
