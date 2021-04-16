using Fusion.Integration;
using Fusion.Integration.Profile;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetResourceOwnerProfile : IRequest<QueryResourceOwnerProfile>
    {
        public GetResourceOwnerProfile(string profileId)
        {
            ProfileId = profileId;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }

        public class Handler : IRequestHandler<GetResourceOwnerProfile, QueryResourceOwnerProfile>
        {
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IMediator mediator;
            private readonly IMemoryCache memoryCache;
            private readonly HttpClient lineOrgClient;

            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IMediator mediator, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.mediator = mediator;
                this.memoryCache = memoryCache;
                this.lineOrgClient = httpClientFactory.CreateClient("lineorg");

            }

            public async Task<QueryResourceOwnerProfile> Handle(GetResourceOwnerProfile request, CancellationToken cancellationToken)
            {
                
                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null)
                    throw new InvalidOperationException($"Could not resolve profile '{request.ProfileId.OriginalIdentifier}'");

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

                // Resolve info from line org, will be cached.. If integration fails null is returned.
                var lineOrgDepartmentProfile = await TryGetRelevantDepartmentsAsync(user.FullDepartment);


                var resourceOwnerProfile = new QueryResourceOwnerProfile(user.FullDepartment, isDepartmentManager, departmentsWithResponsibility, relevantSectors)
                {
                    Sector = sector,
                    ChildDepartments = lineOrgDepartmentProfile?.Children.ToList(),
                    SiblingDepartments = lineOrgDepartmentProfile?.Siblings.ToList()
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
                var delegatedResponsibilities = await mediator.Send(new GetDelegatedDepartmentResponsibilty(user.AzureUniqueId));

                departmentsWithResponsibility.AddRange(delegatedResponsibilities.Select(r => r.DepartmentId));

                return departmentsWithResponsibility;
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
                    .Select(dpt => dpt.DepartmentId);
            }

            private async Task<IEnumerable<string>> ResolveDownstreamSectors(string? department)
            {
                if (department is null)
                    return Array.Empty<string>();

                var departments = await mediator.Send(new GetDepartments().StartsWith(department));
                return departments
                    .Select(dpt => dpt.SectorId!).Distinct();
            }
        

            private async Task<RelevantDepartments?> TryGetRelevantDepartmentsAsync(string? fullDepartment)
            {
                if (fullDepartment is null)
                    return null;

                string cacheKey = $"relevant-departments-{fullDepartment}";

                if (memoryCache.TryGetValue(cacheKey, out RelevantDepartments relevantDepartments))
                    return relevantDepartments;

                try
                {
                    relevantDepartments = await ResolveRelevantDepartmentsAsync(fullDepartment);

                    memoryCache.Set(cacheKey, relevantDepartments, TimeSpan.FromHours(30));
                    return relevantDepartments;
                }
                catch (Exception ex)
                {
                    // Log
                    logger.LogCritical(ex, "Could not resolve relevant departments for {Department}", fullDepartment);
                    return null;
                }

            }

            private async Task<RelevantDepartments> ResolveRelevantDepartmentsAsync(string fullDepartmentPath)
            {
                var relevantDepartments = new RelevantDepartments();

                var currentDepartment = string.Join(" ", fullDepartmentPath.Split(" ").TakeLast(3));
                var parentDepartment = string.Join(" ", fullDepartmentPath.SkipLast(1).TakeLast(3));



                var respCurrentDepartment = lineOrgClient.GetAsync($"lineorg/departments/{currentDepartment}?$expand=children");
                var respParentDepartment = lineOrgClient.GetAsync($"lineorg/departments/{parentDepartment}?$expand=children");

                await Task.WhenAll(respCurrentDepartment, respParentDepartment);

                var respCurrent = await respCurrentDepartment;
                if (respCurrent.IsSuccessStatusCode)
                {
                    var content = await respCurrent.Content.ReadAsStringAsync();
                    var department = JsonConvert.DeserializeAnonymousType(content, new
                    {
                        children = new[] { new { name = string.Empty, fullName = string.Empty } }
                    });
                    relevantDepartments.Children = department.children.Select(d => d.fullName).ToList();
                }

                var respParent = await respParentDepartment;
                if (respParent.IsSuccessStatusCode)
                {
                    var content = await respParent.Content.ReadAsStringAsync();
                    var department = JsonConvert.DeserializeAnonymousType(content, new
                    {
                        children = new[] { new { name = string.Empty, fullName = string.Empty } }
                    });
                    relevantDepartments.Siblings = department.children.Select(d => d.fullName).ToList();
                }

                return relevantDepartments;
            }

            private class RelevantDepartments
            {
                public List<string> Children { get; set; } = new List<string>();
                public List<string> Siblings { get; set; } = new List<string>();
            }
        }
    }
}
