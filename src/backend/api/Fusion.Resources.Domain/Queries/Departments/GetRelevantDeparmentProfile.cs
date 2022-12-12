using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Domain.Models;
using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.MarkedNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Fusion.Resources.Domain.Queries
{
    /// <summary>
    /// Fetch the profile for a resource owner. Will compile a list of departments the person has responsibilities in and which is relevant.
    /// </summary>
    public class GetRelevantDeparmentProfile : IRequest<IEnumerable<QueryRelevantDepartmentProfile>>
    {
        public GetRelevantDeparmentProfile(string profileId, AspNetCore.OData.ODataQueryParams query)
        {
            ProfileId = profileId;
            Query = query;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }
        public ODataQueryParams Query { get; }



        public class Handler : IRequestHandler<GetRelevantDeparmentProfile, IEnumerable<QueryRelevantDepartmentProfile>>
        {
            private readonly TimeSpan defaultAbsoluteCacheExpirationHours = TimeSpan.FromHours(1);
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;
            private readonly IMemoryCache memCache;
            private bool sapIdFound = false;
            List<QueryRelevantDepartmentProfile> lstDepartments = new List<QueryRelevantDepartmentProfile>();

            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IMediator mediator, IMemoryCache memCache)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;
                this.memCache = memCache;

            }
            private CancellationToken _cancellationToken;
            public async Task<IEnumerable<QueryRelevantDepartmentProfile>> Handle(GetRelevantDeparmentProfile request, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null) return null;

                //var activeDepartments = (await lineOrgClient.GetOrgUnitDepartmentsAsync()).ToList();

                // Resolve departments with responsibility
                var departmentsWithAccess = await ResolveDepartmentsWithAccessAsync(user);

                //departmentsWithAccess.Where(x => x.Value.Contains(request.Query.Filter.GetFilterForField("fulldepartment").Value));




                if (user.IsResourceOwner)
                {
                    List<string> Reasons = new List<string>();
                    Reasons.Add("Manager");

                    QueryRelevantDepartmentProfile DepartmentProfile = await GetDepartmentInformation(user.FullDepartment, Reasons, request.Query);
                    StoreResult(lstDepartments, DepartmentProfile);

                }

                foreach (var department in departmentsWithAccess)
                {
                    if (sapIdFound == true)
                    {
                        break;
                    }
                    List<string> Reasons = new List<string>();
                    if (department.Value.Contains("Resources.ResourceOwner") && !department.Key.Contains("*"))
                    {

                        Reasons.Add("DelegatedManager");

                    }
                    else if (department.Value.Contains("Resources.FullControl") && department.Key.Contains("*"))
                    {

                        List<string> reason = new List<string>();
                        reason.Add("ParentManager");
                        var DepChildren = await ResolveChildren(department.Key.Trim('*').TrimEnd());

                        await GetDepartmentChildren(department.Key.Trim('*').TrimEnd(), reason, request.Query);


                        Reasons.Add("Manager ( Wildcard) ");


                    }
                    else if (department.Value.Contains("Resources.FullControl") && !department.Key.Contains("*"))
                    {
                        Reasons.Add("Write Access");

                    }
                    else
                    {

                        Reasons.Add("NOT Manager");

                    }

                    if (Reasons.Count > 0)
                    {
                        QueryRelevantDepartmentProfile? DepartmentProfile = await GetDepartmentInformation(department.Key.Trim('*').TrimEnd(), Reasons, request.Query);
                        await StoreResult(lstDepartments, DepartmentProfile);
                    }


                }

                return lstDepartments;
            }


            private async Task<bool> GetDepartmentChildren(string dep, List<string> reason, ODataQueryParams query)
            {
                if (sapIdFound == true)
                {
                    return true;
                }
                var DepChildren = await ResolveChildren(dep);
                if (DepChildren != null)
                {
                    // recurse over all children categories and add them to the list
                    foreach (var child in DepChildren.Children)
                    {
                        if (sapIdFound == true)
                        {
                            break;
                        }
                        QueryRelevantDepartmentProfile ChildDepartmentProfile = await GetDepartmentInformation(child.DepartmentId, reason, query);
                        await StoreResult(lstDepartments, ChildDepartmentProfile);

                        await GetDepartmentChildren(child.DepartmentId, reason, query);
                    }
                }
                return true;
            }

            private async Task<bool> StoreResult(List<QueryRelevantDepartmentProfile> lstDepartments, QueryRelevantDepartmentProfile? DepartmentProfile)
            {
                if (DepartmentProfile != null)
                {
                    lstDepartments.Add(DepartmentProfile);
                }

                return true;
            }

            private async Task<QueryRelevantDepartmentProfile?> GetDepartmentInformation(string departmentpath, List<string> Reasons, ODataQueryParams query)
            {
                var cacheKey = $"{departmentpath.Replace(" ", "")}";
                QueryRelevantDepartmentProfile? DepartmentProfile = null;
                ApiOrgUnit? departmentInfo;

                if (memCache.TryGetValue(cacheKey, out QueryRelevantDepartmentProfile cachedHeaders))
                {
                    Console.WriteLine("Using cached access info...");
                    return cachedHeaders;
                }
                else
                {
                    departmentInfo = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));
                }


                if (query.HasFilter)
                {

                    //var departmentInfo = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));

                    if (departmentInfo != null)
                    {


                        var name = query.Filter.GetFilterForField("name")?.Value;
                        var sapIdfilter = query.Filter.GetFilterForField("sapId")?.Value;
                        var shortName = query.Filter.GetFilterForField("shortName")?.Value;
                        var department = query.Filter.GetFilterForField("department")?.Value;
                        var parentSapId = query.Filter.GetFilterForField("parentSapId")?.Value;
                        var fulldepartmentfilter = query.Filter.GetFilterForField("fulldepartment")?.Value;



                        if (fulldepartmentfilter != null)
                        {
                            if (departmentInfo.FullDepartment.Contains(fulldepartmentfilter.ToString()))
                            {
                                DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);

                            }
                        }

                        if (sapIdfilter != null)
                        {
                            if (departmentInfo.SapId.Contains(sapIdfilter))
                            {
                                DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);
                                sapIdFound = true;

                            }
                        }


                        if (name != null)
                        {
                            if (departmentInfo.Name.Contains(name.ToString()))
                            {
                                DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);


                            }
                        }
                    }

                }


                if (DepartmentProfile == null)
                {
                    departmentInfo = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));
                    DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);
                }
                memCache.Set(cacheKey, DepartmentProfile, TimeSpan.FromMinutes(5));



                if (memCache.TryGetValue(cacheKey, out QueryRelevantDepartmentProfile test))
                {
                    Console.WriteLine("Cache stored ...");

                }
                return DepartmentProfile;



            }

            private async Task<Dictionary<string, string>> ResolveDepartmentsWithAccessAsync(FusionPersonProfile user)
            {
                var isDepartmentManager = user.IsResourceOwner;

                var departmentsWithResponsibility = new Dictionary<string, string>();

                // Add the current department if the user is resource owner in the department.
                if (isDepartmentManager && user.FullDepartment != null)
                    departmentsWithResponsibility.Add(user.FullDepartment, "Fusion.Resources.ResourceOwner");

                // Add all departments the user has been delegated responsibility for.

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)
                //.WhereRoleName(AccessRoles.ResourceOwner)
                );


                foreach (var role in roleAssignedDepartments)
                {

                    if (role.ValidTo >= DateTimeOffset.Now && role.Scope!.Values != null)
                    {
                        departmentsWithResponsibility.Add(role.Scope.Values.FirstOrDefault(), role.RoleName);
                    }
                }

                //departmentsWithResponsibility.Add(roleAssignedDepartments
                //    .Where(x => x.Scope != null && x.ValidTo >= DateTimeOffset.Now)
                //    .SelectMany(x => x.Scope!.Values))
                //);

                return departmentsWithResponsibility;
            }


            private async Task<QueryRelatedDepartments?> ResolveChildren(string? department)
            {
                if (department is null)
                    return null;

                var departments = await mediator.Send((new GetRelatedDepartments(department)));

                return departments;

            }

        }
    }
}