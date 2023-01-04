using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Services.LineOrg.ApiModels;
using Microsoft.Extensions.Caching.Memory;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetPersonRelevantOrgUnits : IRequest<QueryRangedList<QueryRelevantOrgUnit>>
    {
        public GetPersonRelevantOrgUnits(PersonId personId)
        {
            PersonId = personId;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId PersonId { get; }
        public ODataQueryParams Query { get; set; } = new();

        public GetPersonRelevantOrgUnits WithQuery(ODataQueryParams query)
        {
            Query = query;
            return this;
        }



        public class Handler : IRequestHandler<GetPersonRelevantOrgUnits, QueryRangedList<QueryRelevantOrgUnit>>
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

            public async Task<QueryRangedList<QueryRelevantOrgUnit>> Handle(GetPersonRelevantOrgUnits request, CancellationToken cancellationToken)
            {
                var orgUnits = await GetOrgUnitsAsync();

                var relevantOrgUnits = await QueryAllRelevantOrgUnitsForUser(request, cancellationToken, orgUnits);
                var filteredOrgUnits = ApplyOdataFilters(request.Query.Filter, relevantOrgUnits);

                var totalCount = relevantOrgUnits.Count;

                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(totalCount);

                var pagedQuery = QueryRangedList.FromItems(filteredOrgUnits, filteredOrgUnits.Count, skip);

                return pagedQuery;
            }

            private static List<QueryRelevantOrgUnit> ApplyOdataFilters(ODataExpression filter, List<QueryRelevantOrgUnit> orgUnits)
            {
                var sapIdFilter = filter.GetFilterForField("sapId");
                if (sapIdFilter != null)
                {
                    if (sapIdFilter.Operation != FilterOperation.Eq)
                        throw new ArgumentException("Only the 'eq' operator is supported for field 'sapId'.");

                    orgUnits = orgUnits.Where(x => x.SapId == sapIdFilter.Value).ToList();
                }

                var nameFilter = filter.GetFilterForField("name");
                if (nameFilter != null)
                {
                    if (nameFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Name == nameFilter.Value).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.Contains(nameFilter.Value)).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.StartsWith(nameFilter.Value)).ToList();
                    }

                    else if (nameFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Name.EndsWith(nameFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"The '{nameFilter.Operation}' operator is NOT supported for field 'name'.");
                    }
                }

                var departmentFilter = filter.GetFilterForField("department");
                if (departmentFilter != null)
                {
                    if (departmentFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Department == departmentFilter.Value).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.Contains(departmentFilter.Value)).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.StartsWith(departmentFilter.Value)).ToList();
                    }

                    else if (departmentFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.Department.EndsWith(departmentFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{departmentFilter.Operation}' operator is NOT supported for field 'department'.");
                    }
                }

                var fullDepartmentFilter = filter.GetFilterForField("fulldepartment");
                if (fullDepartmentFilter != null)
                {
                    if (fullDepartmentFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment == fullDepartmentFilter.Value).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.Contains(fullDepartmentFilter.Value)).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.StartsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.StartsWith(fullDepartmentFilter.Value)).ToList();
                    }

                    else if (fullDepartmentFilter.Operation == FilterOperation.EndsWith)
                    {
                        orgUnits = orgUnits.Where(x => x.FullDepartment.EndsWith(fullDepartmentFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{fullDepartmentFilter.Operation}' operator is NOT supported for field 'fullDepartment'.");
                    }
                }

                var reasonFilter = filter.GetFilterForField("reason");
                if (reasonFilter != null)
                {
                    if (reasonFilter.Operation == FilterOperation.Eq)
                    {
                        orgUnits = orgUnits.Where(x => x.Reasons.Contains(reasonFilter.Value)).ToList();
                    }

                    else if (reasonFilter.Operation == FilterOperation.Contains)
                    {
                        orgUnits = orgUnits.Where(x => x.Reasons.Contains(reasonFilter.Value)).ToList();
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The '{reasonFilter.Operation}' operator is NOT supported for field 'reason'.");
                    }
                }

                return orgUnits;
            }

            private async Task<List<QueryRelevantOrgUnit>> QueryAllRelevantOrgUnitsForUser(GetPersonRelevantOrgUnits request,
                CancellationToken cancellationToken, List<QueryRelevantOrgUnit> orgUnits)
            {
                var cacheKey = $"GetPersonRelevantDepartments_{request.PersonId.OriginalIdentifier}";
                if (memoryCache.TryGetValue(cacheKey, out List<QueryRelevantOrgUnit> relevantOrgUnits)) return relevantOrgUnits;

                var user = await profileResolver.ResolvePersonFullProfileAsync(request.PersonId.OriginalIdentifier);
                if (user?.FullDepartment is null) return new List<QueryRelevantOrgUnit>();

                var sector = await ResolveSector(user.FullDepartment);
                var departmentsWithResponsibility = await ResolveDepartmentsWithResponsibilityAsync(user);
                var isDepartmentManager = departmentsWithResponsibility.Any(r => r == user.FullDepartment);
                var relevantSectors = await ResolveRelevantSectorsAsync(user.FullDepartment, sector, isDepartmentManager,
                    departmentsWithResponsibility);

                var relevantDepartments = new List<QueryDepartment>();
                foreach (var relevantSector in relevantSectors)
                    relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));

                var lineOrgDepartmentProfile =
                    await mediator.Send(new GetRelatedDepartments(user.FullDepartment), cancellationToken);

                var adminClaims = user.Roles
                    ?.Where(x => x.Name.StartsWith("Fusion.Resources.Full") || x.Name.StartsWith("Fusion.Resources.Admin"))
                    .Select(x => x).ToList();
                var readClaims = user.Roles
                    ?.Where(x => x.Name.StartsWith("Fusion.Resources.Request") || x.Name.StartsWith("Fusion.Resources.Read"))
                    .Select(x => x).ToList();

                var retList = new List<QueryFullDepartmentReasonRef>();
                if (isDepartmentManager)
                    retList.Add(
                        new QueryFullDepartmentReasonRef { FullDepartment = user.FullDepartment!, Reason = "ResourceOwner" });
                if (adminClaims is not null) retList.AddRange(adminClaims.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.Scope?.Value ?? "*", Reason = dep.Name }));
                if (readClaims is not null) retList.AddRange(readClaims.Select(dep => new QueryFullDepartmentReasonRef { FullDepartment = dep.Scope?.Value ?? "*", Reason = "Read" }));
                retList.AddRange(departmentsWithResponsibility.Select(dep => new QueryFullDepartmentReasonRef
                { FullDepartment = dep, Reason = "DelegatedManager" }));
                retList.AddRange(relevantSectors.Select(dep => new QueryFullDepartmentReasonRef
                { FullDepartment = dep, Reason = "RelevantSector" }));
                retList.AddRange(relevantDepartments.Select(dep => new QueryFullDepartmentReasonRef
                { FullDepartment = dep.DepartmentId, Reason = "RelevantDepartment" }));
                retList.AddRange(lineOrgDepartmentProfile?.Children.Select(dep => new QueryFullDepartmentReasonRef
                { FullDepartment = dep.DepartmentId, Reason = "RelevantChild" }) ??
                                 Array.Empty<QueryFullDepartmentReasonRef>());
                retList.AddRange(lineOrgDepartmentProfile?.Siblings.Select(dep => new QueryFullDepartmentReasonRef
                { FullDepartment = dep.DepartmentId, Reason = "RelevantSibling" }) ??
                                 Array.Empty<QueryFullDepartmentReasonRef>());

                var endResult = new List<QueryRelevantOrgUnit>();

                foreach (var queryRef in retList)
                {
                    if (queryRef.IsWildCard)
                    {
                        var orgUnitsForReference = orgUnits
                            .Where(x => x.FullDepartment.StartsWith(queryRef.FullDepartment.Replace("*", "").Trim())).ToList();
                        MergeOrgUnitReasons(orgUnitsForReference, endResult, queryRef);
                    }
                    else
                    {
                        var orgUnitsForReference =
                            orgUnits.Where(x => x.FullDepartment == queryRef.FullDepartment).ToList();
                        MergeOrgUnitReasons(orgUnitsForReference, endResult, queryRef);
                    }
                }

                memoryCache.Set(cacheKey, endResult, TimeSpan.FromMinutes(10));

                return endResult;
            }

            private static void MergeOrgUnitReasons(List<QueryRelevantOrgUnit> referenceList, List<QueryRelevantOrgUnit> endResult, QueryFullDepartmentReasonRef currentReference)
            {
                foreach (var ouRef in referenceList)
                {
                    var alreadyInList = endResult.FirstOrDefault(x => x.SapId == ouRef.SapId);
                    if (alreadyInList is null)
                    {
                        if (ouRef.Reasons.Contains(currentReference.Reason) == false)
                            ouRef.Reasons.Add(currentReference.Reason);
                        endResult.Add(ouRef);
                    }
                    else
                    {
                        if (ouRef.Reasons.Contains(currentReference.Reason) == false)
                            alreadyInList.Reasons.Add(currentReference.Reason);
                    }
                }
            }

            private async Task<List<QueryRelevantOrgUnit>> GetOrgUnitsAsync()
            {
                const string cacheKey = "Cache.GetOrgUnitsAsync";
                var orgUnits = await memoryCache.GetOrCreateAsync(cacheKey, async (entry) =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                    var orgUnitResponse =
                        await lineOrgClient.GetFromJsonAsync<ApiPagedCollection<QueryRelevantOrgUnit>>("/org-units?$top=50000");
                    if (orgUnitResponse is null)
                        throw new InvalidOperationException("Could not fetch org units from line org");

                    return orgUnitResponse.Value.ToList();
                });
                // Flush reason due to caching option.
                foreach (var item in orgUnits)
                    item.Reasons = new List<string>();

                return orgUnits;
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