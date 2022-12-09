using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


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
            private readonly ILogger<Handler> logger;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IFusionRolesClient rolesClient;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;

            public Handler(ILogger<Handler> logger, IFusionProfileResolver profileResolver, IFusionRolesClient rolesClient, ILineOrgResolver lineOrgResolver, IMediator mediator)
            {
                this.logger = logger;
                this.profileResolver = profileResolver;
                this.rolesClient = rolesClient;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;

            }

            public async Task<IEnumerable<QueryRelevantDepartmentProfile>> Handle(GetRelevantDeparmentProfile request, CancellationToken cancellationToken)
            {
                var user = await profileResolver.ResolvePersonBasicProfileAsync(request.ProfileId.OriginalIdentifier);

                if (user is null) return null;

                //var activeDepartments = (await lineOrgClient.GetOrgUnitDepartmentsAsync()).ToList();

                // Resolve departments with responsibility
                var departmentsWithAccess = await ResolveDepartmentsWithAccessAsync(user);
                List<QueryRelevantDepartmentProfile> lstDepartments = new List<QueryRelevantDepartmentProfile>();

                if (user.IsResourceOwner)
                {
                    List<string> Reasons = new List<string>();
                    Reasons.Add("Manager");

                    QueryRelevantDepartmentProfile DepartmentProfile = await GetDepartmentInformation(user.FullDepartment, Reasons, request.Query);
                    StoreResult(lstDepartments, DepartmentProfile);

                }

                foreach (var department in departmentsWithAccess)
                {
                    List<string> Reasons = new List<string>();
                    if (department.Value.Contains("Resources.ResourceOwner") && !department.Key.Contains("*"))
                    {

                        Reasons.Add("DelegatedManager");

                    }
                    else if (department.Value.Contains("Resources.FullControl") && department.Key.Contains("*"))
                    {


                        var DepChildren = await ResolveChildren(department.Key.Trim('*').TrimEnd());

                        foreach (var item in DepChildren.Children)
                        {
                            List<string> parentReason = new List<string>();
                            parentReason.Add("ParentManager");

                            QueryRelevantDepartmentProfile DepDepartmentProfile = await GetDepartmentInformation(item.DepartmentId.Trim('*').TrimEnd(), parentReason, request.Query);

                            StoreResult(lstDepartments, DepDepartmentProfile);

                            var ChildrenDepChildren = await ResolveChildren(item.DepartmentId.Trim('*').TrimEnd());
                            if (ChildrenDepChildren?.Children.Count != 0)
                            {
                                List<string> ParentParentReason = new List<string>();
                                ParentParentReason.Add("ParentParentManager");

                                foreach (var childchild in ChildrenDepChildren.Children)
                                {
                                    QueryRelevantDepartmentProfile ChildDepartmentProfile = await GetDepartmentInformation(childchild.DepartmentId, ParentParentReason, request.Query);

                                    StoreResult(lstDepartments, DepDepartmentProfile);
                                }

                            }
                        }

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
                        StoreResult(lstDepartments, DepartmentProfile);
                    }


                }

                return lstDepartments;
            }

            private static void StoreResult(List<QueryRelevantDepartmentProfile> lstDepartments, QueryRelevantDepartmentProfile? DepartmentProfile)
            {
                if (DepartmentProfile != null)
                {
                    lstDepartments.Add(DepartmentProfile);
                }
            }

            private async Task<QueryRelevantDepartmentProfile?> GetDepartmentInformation(string departmentpath, List<string> Reasons, ODataQueryParams query)
            {
                var departmentInfo = await lineOrgResolver.ResolveOrgUnitAsync(DepartmentId.FromFullPath(departmentpath));
                if (departmentInfo != null)
                {

                    if (query.Filter.GetFilters().Count() > 0)
                    {
                        if (query.Filter.GetFilterForField("fulldepartment") != null)
                        {
                            if (departmentInfo.FullDepartment.Contains(query.Filter.GetFilterForField("fulldepartment").Value))
                            {
                                var DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);
                                return DepartmentProfile;
                            }
                        }

                        if (query.Filter.GetFilterForField("sapId") != null)
                        {
                            if (departmentInfo.SapId.Contains(query.Filter.GetFilterForField("sapId").Value))
                            {
                                var DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);
                                return DepartmentProfile;
                            }
                        }
                    }
                    else
                    {
                        var DepartmentProfile = new QueryRelevantDepartmentProfile(departmentpath, Reasons, departmentInfo?.SapId, "parentSapId", departmentInfo?.ShortName, departmentInfo?.Department, departmentInfo?.Name);
                        return DepartmentProfile;
                    }
                }


                return null;
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