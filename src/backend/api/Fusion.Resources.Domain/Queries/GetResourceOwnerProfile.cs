﻿using Fusion.Integration;
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
using Fusion.Resources.Domain.Models;

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
                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null) return null;

                var sector = await ResolveSector(user.FullDepartment);

                // Resolve departments with responsibility
                var departmentsWithResponsibility = await ResolveDepartmentsWithResponsibilityAsync(user);

                // Determine if the user is a manager in the department he/she belongs to.
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r == user.FullDepartment);

                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager, departmentsWithResponsibility);

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

            private async Task<List<string>> ResolveRelevantSectorsAsync(string? fullDepartment, string? sector, bool isDepartmentManager, IEnumerable<string> departmentsWithResponsibility)
            {
                // Get sectors the user have responsibility in, to find all relevant departments
                var relevantSectors = new List<string>();
                foreach (var department in departmentsWithResponsibility)
                {
                    var resolvedSector = await ResolveSector(department);
                    if (resolvedSector != null)
                    {
                        relevantSectors.Add(resolvedSector);
                    }
                }

                // If the sector does not exist, the person might be higher up.
                if (sector is null && isDepartmentManager)
                {
                    var downstreamSectors = await ResolveDownstreamSectors(fullDepartment);
                    foreach (var department in downstreamSectors)
                    {
                        var resolvedSector = await ResolveSector(department);
                        if (resolvedSector != null)
                        {
                            relevantSectors.Add(resolvedSector);
                        }
                    }
                }

                return relevantSectors
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            private async Task<List<string>> ResolveDepartmentsWithResponsibilityAsync(FusionPersonProfile user)
            {
                var isDepartmentManager = user.IsResourceOwner;

                var departmentsWithResponsibility = new List<string>();

                // Add the current department if the user is resource owner in the department.
                if (isDepartmentManager && user.FullDepartment != null)
                    departmentsWithResponsibility.Add(user.FullDepartment);

                // Add all departments the user has been delegated responsibility for.
                var delegatedResponsibilities = db.DelegatedDepartmentResponsibles
                    .Where(r => r.ResponsibleAzureObjectId == user.AzureUniqueId)
                    .Where(r => r.DateFrom < DateTime.Now && r.DateTo > DateTime.Now)
                    .Select(r => new QueryDepartmentResponsible(r));

                departmentsWithResponsibility.AddRange(delegatedResponsibilities.Select(r => r.DepartmentId)!);

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

            private async Task<string?> ResolveSector(string? department)
            {
                if (string.IsNullOrEmpty(department))
                    return null;

                var request = new GetDepartmentSector(department);
                return await mediator.Send(request);
            }

            private async Task<IEnumerable<string>> ResolveSectorDepartments(string sector)
            {
                var departments = await mediator.Send(new GetDepartments().InSector(sector));
                return departments
                    .Select(dpt => dpt.FullDepartment);
            }

            private async Task<IEnumerable<string>> ResolveDownstreamSectors(string? department)
            {
                if (department is null)
                    return Array.Empty<string>();

                var departments = await mediator.Send(new GetDepartments().StartsWith(department));
                return departments
                    .Select(dpt => dpt.SectorId!).Distinct();
            }
        }
    }
}