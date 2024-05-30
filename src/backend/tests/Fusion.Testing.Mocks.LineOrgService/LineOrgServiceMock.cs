using Fusion.Events;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Domain;
using Fusion.Services.LineOrg.ApiModels;
using Fusion.Testing.Mocks.LineOrgService.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;

namespace Fusion.Testing.Mocks.LineOrgService
{
    public class LineOrgServiceMock
    {
        readonly WebApplicationFactory<Startup> factory;

        internal static ConcurrentBag<ApiLineOrgUser> Users = new ConcurrentBag<ApiLineOrgUser>();
        internal static ConcurrentBag<ApiDepartment> Departments = new ConcurrentBag<ApiDepartment>();
        internal static ConcurrentBag<ApiOrgUnit> OrgUnits = new ConcurrentBag<ApiOrgUnit>();

        public LineOrgServiceMock()
        {
            factory = new WebApplicationFactory<Startup>();
        }

        public HttpClient CreateHttpClient()
        {
            var client = factory.CreateClient();
            return client;
        }

        public static FusionTestUserBuilder AddTestUser() => new FusionTestUserBuilder();
        public static void AddDepartment(string fullName, string[] children = null)
        {
            var childRefs = children?
                .Select(x => new ApiDepartment.ApiDepartmentRef() { Name = new DepartmentPath(x).GetShortName(), FullName = x })
                .ToList();

            var name = new DepartmentPath(fullName);

            if (Departments.FirstOrDefault(x => string.Equals(x.Name, fullName, StringComparison.OrdinalIgnoreCase)) == null)
                Departments.Add(new ApiDepartment { Name = name.GetShortName(), FullName = fullName, Children = childRefs });


            // Add entry to org unit as well.
            // Ignoring parents for now
            AddOrgUnit(fullName);
        }
        public static void UpdateDepartmentManager(string name, ApiLineOrgUser manager)
        {
            name = new DepartmentPath(name).GetShortName();
            var dep = Departments.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (dep == null)
                return;

            dep.Manager = manager;
        }
        public static ApiOrgUnit AddOrgUnit(string sapId, string name, string department, string fullDepartment, string shortname)
        {
            var item = OrgUnits.FirstOrDefault(x => x.SapId == sapId);
            if (item == null)
            {
                // Need to add the business unit element, as this is used in authorization logic
                var orgPath = !string.IsNullOrEmpty(fullDepartment) ? DepartmentId.FromFullPath(fullDepartment) : DepartmentId.Empty;

                item = new ApiOrgUnit()
                {
                    SapId = sapId,
                    Name = name,
                    Department = department,
                    FullDepartment = fullDepartment,
                    ShortName = shortname,
                    BusinessArea = new ApiOrgUnitRef()
                    {
                        Name = orgPath.BusinessArea,
                        ShortName = orgPath.BusinessArea,
                        FullDepartment = orgPath.BusinessArea,
                        Level = 1
                    }
                };
                OrgUnits.Add(item);
            }

            return item;
        }

        /// <summary>
        /// Not ment for consumption, but temp workaround during department refactor.
        /// </summary>
        /// <param name="fullDepartment"></param>
        public static ApiOrgUnit AddOrgUnit(string fullDepartment)
        {
            var sapId = $"{Math.Abs(HashUtils.HashTextAsInt(fullDepartment))}";

            var name = new DepartmentPath(fullDepartment);
            return AddOrgUnit($"{sapId}", fullDepartment, name.GetShortName(), fullDepartment, fullDepartment.Split(' ').LastOrDefault());
        }
    }

    public class FusionTestUserBuilder
    {
        private readonly ApiLineOrgUser user;
        internal FusionTestUserBuilder()
        {
            user = FusionLineOrgTestData.CreateTestUser();
        }

        public FusionTestUserBuilder AsResourceOwner()
        {
            user.IsResourceOwner = true;
            return this;
        }
        public FusionTestUserBuilder WithDepartment(string department)
        {
            user.Department = department;
            return this;
        }
        public FusionTestUserBuilder WithFullDepartment(string fullDepartment)
        {
            user.Department = new DepartmentPath(fullDepartment).GetShortName();
            user.FullDepartment = fullDepartment;
            return this;
        }

        public FusionTestUserBuilder WithManager(ApiPersonProfileV3 manager)
        {
            user.Manager = new ApiLineOrgManager { AzureUniqueId = manager.AzureUniqueId!.Value, Department = manager.Department, Mail = manager.Mail, FullDepartment = manager.FullDepartment, Name = manager.Name };
            user.ManagerId = manager.AzureUniqueId;
            return this;
        }

        public FusionTestUserBuilder MergeWithProfile(ApiPersonProfileV3 testProfile)
        {
            user.AzureUniqueId = testProfile.AzureUniqueId!.Value;
            user.Mail = testProfile.Mail;
            user.IsResourceOwner = testProfile.IsResourceOwner;
            user.Department = testProfile.Department;
            user.FullDepartment = testProfile.FullDepartment;
            user.Name = testProfile.Name;
            user.JobTitle = testProfile.JobTitle;
            return this;
        }
        public ApiLineOrgUser SaveProfile()
        {
            var exists = LineOrgServiceMock.Users.Any(x => x.AzureUniqueId == user.AzureUniqueId);
            if (!exists)
                LineOrgServiceMock.Users.Add(user);

            LineOrgServiceMock.AddDepartment(user.FullDepartment);
            if (user.IsResourceOwner)
                LineOrgServiceMock.UpdateDepartmentManager(user.FullDepartment, user);

            LineOrgServiceMock.AddOrgUnit(user.FullDepartment);
            return user;
        }
    }

}
