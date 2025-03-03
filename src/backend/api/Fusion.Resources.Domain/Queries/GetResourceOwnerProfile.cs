using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetResourceOwnerProfile : IRequest<QueryResourceOwnerProfile?>
    {
        public GetResourceOwnerProfile(string profileId)
        {
            ProfileId = profileId;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }

        public class Handler : IRequestHandler<GetResourceOwnerProfile, QueryResourceOwnerProfile?>
        {
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ResourcesDbContext db;

            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, IMediator mediator, ResourcesDbContext db)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.db = db;
            }

            public async Task<QueryResourceOwnerProfile?> Handle(GetResourceOwnerProfile request, CancellationToken cancellationToken)
            {
                var user = await profileResolver.ResolvePersonFullProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null) return null;

                var userIsManagerFor = await GetUserManagerForAsync(user);
                var sector = await ResolveSector(user, userIsManagerFor);

                // Resolve departments with responsibility
                var departmentsWithResponsibility = await ResolveDepartmentsWithResponsibilityAsync(user, userIsManagerFor);

                // Determine if the user is a manager in the department he/she belongs to.
                var isDepartmentManager = departmentsWithResponsibility.Any();

                var relevantSectors = ResolveRelevantSectors(departmentsWithResponsibility);

                var relevantDepartments = new List<string>();
                foreach (var relevantSector in relevantSectors)
                {
                    relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));
                }

                QueryRelatedDepartments? lineOrgDepartmentProfile = null;
                if (!string.IsNullOrEmpty(user.FullDepartment))
                    lineOrgDepartmentProfile = await mediator.Send(new GetRelatedDepartments(user.FullDepartment), cancellationToken);
                else
                    logger.LogDebug("No department found for profile. Skipping related department check.");

                var resourceOwnerProfile = new QueryResourceOwnerProfile(user.FullDepartment, isDepartmentManager, departmentsWithResponsibility, relevantSectors)
                {
                    Sector = sector,
                    ChildDepartments = lineOrgDepartmentProfile?.Children.Select(x => x.FullDepartment).ToList(),
                    SiblingDepartments = lineOrgDepartmentProfile?.Siblings.Select(x => x.FullDepartment).ToList()
                };

                return resourceOwnerProfile;
            }

            private async Task<List<string>> GetUserManagerForAsync(FusionFullPersonProfile user)
            {
                var orgUnits = new List<string>();

                var managerRoles = (user.Roles ?? new List<FusionRole>())
                    .Where(x => string.Equals(x.Name, "Fusion.LineOrg.Manager", StringComparison.OrdinalIgnoreCase))
                    .Where(x => !string.IsNullOrEmpty(x.Scope?.Value))
                    .Select(x => x.Scope?.Value!)
                    .ToList();

                foreach (var orgUnitId in managerRoles)
                {
                    var orgUnit = await mediator.Send(new ResolveLineOrgUnit(orgUnitId));
                    if (orgUnit?.FullDepartment != null)
                    {
                        orgUnits.Add(orgUnit.FullDepartment);
                    }
                }

                return orgUnits;
            }
            private List<string> ResolveRelevantSectors(IEnumerable<string> departmentsWithResponsibility)
            {
                // Get sectors the user have responsibility in, to find all relevant departments
                var relevantSectors = new List<string>();
                foreach (var department in departmentsWithResponsibility)
                {
                    var resolvedSector = GetSectorForDepartment(department);
                    if (resolvedSector != null)
                    {
                        relevantSectors.Add(resolvedSector);
                    }
                }

                return relevantSectors
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            private string? GetSectorForDepartment(string department)
            {
                var path = new DepartmentPath(department);
                var sector = (path.Level > 1) ? path.Parent() : null;
                return sector;
            }

            private async Task<List<string>> ResolveDepartmentsWithResponsibilityAsync(FusionFullPersonProfile user, List<string> userIsManagerFor)
            {
                var departmentsWithResponsibility = new List<string>();

                // Add the current department if the user is resource owner in the department.
                departmentsWithResponsibility.AddRange(userIsManagerFor);

                // Add departments the user has been delegated responsibility for.
                departmentsWithResponsibility.AddRange(await ResolveDelegatedResponsibilities(user));

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)
                    .WhereRoleName(AccessRoles.ResourceOwner)
                );

                departmentsWithResponsibility.AddRange(roleAssignedDepartments
                    .Where(x => x.Scope != null && x.ValidTo >= DateTimeOffset.Now)
                    .SelectMany(x => x.Scope!.Values)
                );

                return departmentsWithResponsibility.Distinct().ToList();
            }

            private async Task<string?> ResolveSector(FusionFullPersonProfile profile, List<string> userIsManagerFor)
            {
                if (profile.IsResourceOwner)
                    return profile.FullDepartment;

                if (string.IsNullOrEmpty(profile.FullDepartment))
                    return null;

                return await mediator.Send(new GetDepartmentSector(profile.FullDepartment));
            }



            private async Task<IEnumerable<string>> ResolveSectorDepartments(string sector)
            {
                var departments = await mediator.Send(new GetDepartments().InSector(sector));
                return departments
                    .Select(dpt => dpt.FullDepartment);
            }

            // This method returns departments where the user has delegated responsibilities AND the department
            // exists. The PIMS sync doesn't currently remove delegated responsibilities when a department has
            // been removed.
            private async Task<IEnumerable<string>> ResolveDelegatedResponsibilities(FusionFullPersonProfile user)
            {
                // Get all departments the user has been delegated responsibility for.
                var delegatedResponsibilities = db.DelegatedDepartmentResponsibles
                    .Where(r => r.ResponsibleAzureObjectId == user.AzureUniqueId)
                    .Where(r => r.DateFrom < DateTime.Now && r.DateTo > DateTime.Now)
                    .Select(r => r.DepartmentId);

                // Only select responsibilities for existing departments
                if (delegatedResponsibilities.Any())
                {
                    var validDepartments = await mediator.Send(new GetDepartments().ByIds(delegatedResponsibilities));
                    return validDepartments.Select(d => d.FullDepartment);
                }

                return delegatedResponsibilities;
            }
        }
    }
}
