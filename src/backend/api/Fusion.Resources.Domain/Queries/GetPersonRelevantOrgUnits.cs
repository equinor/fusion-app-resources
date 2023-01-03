using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.Extensions.Caching.Memory;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetPersonRelevantOrgUnits : IRequest<List<ApiOrgUnit>>
    {
        public GetPersonRelevantOrgUnits(PersonId personId)
        {
            PersonId = personId;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId PersonId { get; }

        public class Handler : IRequestHandler<GetPersonRelevantOrgUnits, List<ApiOrgUnit>>
        {
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly IMemoryCache memoryCache;
            private readonly HttpClient lineOrgClient;

            public Handler(IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, IMediator mediator, IMemoryCache memoryCache, IHttpClientFactory httpClientFactory)
            {
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.memoryCache = memoryCache;
                lineOrgClient = httpClientFactory.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg());
            }

            public async Task<List<ApiOrgUnit>> Handle(GetPersonRelevantOrgUnits request, CancellationToken cancellationToken)
            {

                var orgUnits = await GetOrgUnitsAsync();

                var cacheKey = $"GetPersonRelevantDepartments_{request.PersonId}";
                if (memoryCache.TryGetValue(cacheKey, out List<ApiOrgUnit> relevantOrgUnits)) return relevantOrgUnits;

                var user = await profileResolver.ResolvePersonFullProfileAsync(request.PersonId.OriginalIdentifier);
                if (user?.FullDepartment is null) return new List<ApiOrgUnit>();

                var sector = await ResolveSector(user.FullDepartment);
                var departmentsWithResponsibility = await ResolveDepartmentsWithResponsibilityAsync(user);
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r == user.FullDepartment);
                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager, departmentsWithResponsibility);

                var relevantDepartments = new List<QueryDepartment>();
                foreach (var relevantSector in relevantSectors) relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));

                var lineOrgDepartmentProfile = await mediator.Send(new GetRelatedDepartments(user.FullDepartment), cancellationToken);

                var adminClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin")).Select(x => x);
                var readClaims = user.Roles?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read")).Select(x => x);

                var retList = new List<QueryFullDepartmentReasonRef>();
                if (isDepartmentManager) retList.Add(new QueryFullDepartmentReasonRef { FullDepartment = user.FullDepartment!, Reason = "ResourceOwner" });
                if (adminClaims is not null) retList.AddRange(adminClaims.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.Scope?.Value ?? "*", Reason = "Write" }));
                if (readClaims is not null) retList.AddRange(readClaims.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.Scope?.Value ?? "*", Reason = "Read" }));
                retList.AddRange(departmentsWithResponsibility.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep, Reason = "DelegatedManager" }));
                retList.AddRange(relevantSectors.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep, Reason = "RelevantSector" }));
                retList.AddRange(relevantDepartments.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.DepartmentId, Reason = "RelevantDepartment" }));
                retList.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.DepartmentId, Reason = "RelevantChild" }) ?? Array.Empty<QueryFullDepartmentReasonRef>());
                retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.DepartmentId, Reason = "RelevantSibling" }) ?? Array.Empty<QueryFullDepartmentReasonRef>());

                var endResult = new List<ApiOrgUnit>();

                foreach (var queryRef in retList)
                {
                    endResult.AddRange(queryRef.FullDepartment.Contains("*")
                        ? orgUnits.Where(x =>
                            x.FullDepartment.StartsWith(queryRef.FullDepartment.Replace("*", "").Trim()))
                        : orgUnits.Where(x => x.FullDepartment == queryRef.FullDepartment));
                }

                var distinctOrgUnits = endResult.DistinctBy(x => x.SapId).OrderBy(x => x.SapId).ToList();
                memoryCache.Set(cacheKey, distinctOrgUnits, TimeSpan.FromMinutes(10));

                return distinctOrgUnits;

            }

            private async Task<List<ApiOrgUnit>> GetOrgUnitsAsync()
            {
                const string cacheKey = "Cache.GetOrgUnitsAsync";
                var orgUnits = await memoryCache.GetOrCreateAsync(cacheKey, async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                    var orgUnitResponse =
                        await lineOrgClient.GetFromJsonAsync<ApiPagedCollection<ApiOrgUnit>>("/org-units?$top=50000");
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");

                    return orgUnitResponse.Value;
                });
                return orgUnits.ToList();
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
                        var resolvedSector = await ResolveSector(department.DepartmentId);
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
                var departmentsWithResponsibility = new List<string>();
                // Add all departments the user has been delegated responsibility for.

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)
                    .WhereRoleName(AccessRoles.ResourceOwner)
                );

                departmentsWithResponsibility.AddRange(roleAssignedDepartments
                    .Where(x => x.Scope != null && x.ValidTo >= DateTimeOffset.Now)
                    .SelectMany(x => x.Scope!.Values)
                );

                return departmentsWithResponsibility;
            }

            private async Task<string?> ResolveSector(string? department)
            {
                if (string.IsNullOrEmpty(department))
                    return null;

                var request = new GetDepartmentSector(department);
                return await mediator.Send(request);
            }

            private async Task<IEnumerable<QueryDepartment>> ResolveSectorDepartments(string sector)
            {
                var departments = await mediator.Send(new GetDepartments().InSector(sector));
                return departments
                    .DistinctBy(dpt => dpt.DepartmentId);
            }

            private async Task<IEnumerable<QueryDepartment>> ResolveDownstreamSectors(string? department)
            {
                if (department is null)
                    return Array.Empty<QueryDepartment>();

                var departments = await mediator.Send(new GetDepartments().StartsWith(department));
                return departments.DistinctBy(x => x.DepartmentId);
            }
        }
    }
}