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
    public class GetRelevantOrgUnits : IRequest<IEnumerable<QueryRelevantDepartmentProfile?>>
    {
        public GetRelevantOrgUnits(string profileId, AspNetCore.OData.ODataQueryParams query)
        {
            ProfileId = profileId;
            Query = query;
        }

        /// <summary>
        /// Mail or azure unique id
        /// </summary>
        public PersonId ProfileId { get; set; }
        public ODataQueryParams Query { get; }

        public enum Roles
        {
            DelegatedManager,
            ParentManager,
            Manager,
            Winter
        }

        public class Handler : IRequestHandler<GetRelevantOrgUnits, IEnumerable<QueryRelevantDepartmentProfile>>
        {
            private readonly TimeSpan defaultAbsoluteCacheExpirationHours = TimeSpan.FromHours(1);
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;
            private readonly IMemoryCache memCache;
            private bool sapIdFound = false;
            List<QueryRelevantDepartmentProfile?> lstDepartments = new List<QueryRelevantDepartmentProfile?>();

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
            public async Task<IEnumerable<QueryRelevantDepartmentProfile>?> Handle(GetRelevantOrgUnits request, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null) return null;

                // Resolve departments with responsibility
                var departmentsWithAccess = await ResolveDepartmentsWithAccessAsync(user);

                if (user.IsResourceOwner)
                {
                    List<string> Reasons = new List<string>();
                    Reasons.Add(Roles.Manager.ToString());

                    if (user.FullDepartment != null)
                    {
                        QueryRelevantDepartmentProfile? DepartmentProfile = await GetDepartmentInformation(user.FullDepartment, Reasons, request.Query);
                        StoreResult(lstDepartments, DepartmentProfile);
                    }
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

                        Reasons.Add(Roles.DelegatedManager.ToString());

                    }
                    else if (department.Value.Contains("Resources.FullControl") && department.Key.Contains("*"))
                    {

                        List<string> reason = new List<string>();
                        reason.Add(Roles.ParentManager.ToString());
                        var DepChildren = await GetChildrenAsync(department.Key.Trim('*').TrimEnd());

                        await GetDepartmentChildrenAsync(department.Key.Trim('*').TrimEnd(), reason, request.Query);

                        Reasons.Add(Roles.Manager.ToString());

                    }
                    else if (department.Value.Contains("Resources.FullControl") && !department.Key.Contains("*"))
                    {
                        Reasons.Add("Write Access");
                    }

                    if (Reasons.Count > 0)
                    {
                        QueryRelevantDepartmentProfile? DepartmentProfile = await GetDepartmentInformation(department.Key.Trim('*').TrimEnd(), Reasons, request.Query);
                         StoreResult(lstDepartments, DepartmentProfile);
                    }

                }

                return lstDepartments;
            }


            private async Task<bool> GetDepartmentChildrenAsync(string dep, List<string> reason, ODataQueryParams query)
            {
                if (sapIdFound == true)
                {
                    return true;
                }
                var DepChildren = await GetChildrenAsync(dep);
                if (DepChildren != null)
                {
                    // recurse over all children categories and add them to the list
                    foreach (var child in DepChildren.Children)
                    {
                        if (sapIdFound == true)
                        {
                            break;
                        }
                        QueryRelevantDepartmentProfile? ChildDepartmentProfile = await GetDepartmentInformation(child.DepartmentId, reason, query);
                         StoreResult(lstDepartments, ChildDepartmentProfile);

                        await GetDepartmentChildrenAsync(child.DepartmentId, reason, query);
                    }
                }
                return true;
            }

            private  static void StoreResult(List<QueryRelevantDepartmentProfile?> lstDepartments, QueryRelevantDepartmentProfile? DepartmentProfile)
            {
                if (DepartmentProfile != null)
                {
                    lstDepartments.Add(DepartmentProfile);
                }

            }

            private async Task<QueryRelevantDepartmentProfile?> GetDepartmentInformation(string departmentpath, List<string> Reasons, ODataQueryParams query)
            {
                var cacheKey = $"{departmentpath.Replace(" ", "")}";
                QueryRelevantDepartmentProfile? DepartmentProfile = null;
                ApiOrgUnit? departmentInfo;


                var incache = memCache.TryGetValue(cacheKey, out departmentInfo);

                if (incache == true)
                {
                    Console.WriteLine($"======================== FOUND CACHED {departmentInfo?.FullDepartment} ==========================================");

                }
                else
                {
                    departmentInfo = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));
                    

 

                    var stored = memCache.Set(cacheKey, departmentInfo, defaultAbsoluteCacheExpirationHours);
                    if (stored != null)
                    {
                        Console.WriteLine($"======================== STORED CACHED {stored.FullDepartment}  ==========================================");
                    }
                }

                //departmentInfo = await memCache.GetOrCreateAsync(cacheKey, async entry =>
                //{
                //    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                //    entry.AddExpirationToken(new CancellationChangeToken(CommonLibConstants.CommonLibCacheTokenSource.Token));
                //    var data = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));


                //    return data;
                //});

                //Thread.Sleep(TimeSpan.FromSeconds(10));

                if (query.HasFilter)
                {



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
                            if (departmentInfo.FullDepartment.Contains(fulldepartmentfilter.ToString()) && query.Filter.GetFilterForField("fulldepartment").Operation == FilterOperation.Contains)
                            {
                                DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);

                            }
                            else if (departmentInfo.FullDepartment.Equals(fulldepartmentfilter.ToString()) && query.Filter.GetFilterForField("fulldepartment").Operation.Equals("Eq"))
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


                return DepartmentProfile;



            }

            private async Task<Dictionary<string, string>> ResolveDepartmentsWithAccessAsync(FusionPersonProfile user)
            {
                var isDepartmentManager = user.IsResourceOwner;

                var departmentsWithResponsibility = new Dictionary<string, string>();

                // Add the current department if the user is resource owner in the department.
                if (isDepartmentManager && user.FullDepartment != null)
                    departmentsWithResponsibility.Add(user.FullDepartment, "Fusion.Resources.ResourceOwner");

                // Add all departments the user has Access To.

                var roleAssignedDepartments = await rolesClient.GetRolesAsync(q => q
                    .WherePersonAzureId(user.AzureUniqueId!.Value)

                );


                foreach (var role in roleAssignedDepartments)
                {

                    if (role.ValidTo >= DateTimeOffset.Now && role.Scope!.Values != null)
                    {
                        departmentsWithResponsibility.Add(role.Scope.Values?.FirstOrDefault(), role.RoleName);
                    }
                }

                return departmentsWithResponsibility;
            }


            private async Task<QueryRelatedDepartments?> GetChildrenAsync(string? department)
            {
                if (department is null)
                    return null;

                var departments = await mediator.Send((new GetRelatedDepartments(department)));

                return departments;

            }

        }
    }
}