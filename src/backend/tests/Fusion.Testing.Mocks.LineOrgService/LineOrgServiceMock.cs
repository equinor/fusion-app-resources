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
        }
        public static void UpdateDepartmentManager(string name, ApiLineOrgUser manager)
        {
            name = new DepartmentPath(name).GetShortName();
            var dep = Departments.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (dep == null)
                return;

            dep.Manager = manager;
        }
        public static void AddOrgUnit(string sapId, string name, string department, string fullDepartment, string shortname)
        {
            if (OrgUnits.FirstOrDefault(x => x.SapId == sapId) == null)
            {
                OrgUnits.Add(new ApiOrgUnit()
                {
                    SapId = sapId,
                    Name = name,
                    Department = department,
                    FullDepartment = fullDepartment,
                    ShortName = shortname
                });
            }
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
            return user;
        }
    }

}
