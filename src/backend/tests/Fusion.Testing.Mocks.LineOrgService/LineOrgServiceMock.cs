﻿using Fusion.Events;
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
using Fusion.Hashing;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

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

        /// Be careful not to mutate the original data by accident
        public static ApiDepartment[] GetDepartments()
        {
            return Departments.ToArray();
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
            var orgUnit = AddOrgUnit(fullName);
            // Add children if provided
            if (children is not null)
            {
                foreach (var child in children)
                {
                    var childUnit = AddOrgUnit(child);
                    childUnit.ParentSapId = orgUnit.SapId;
                }
            }
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
    
        public static void AddOrgUnitManager(string fullDepartment, ApiPersonProfileV3 user)
        {
            var orgUnit = OrgUnits.FirstOrDefault(x => x.FullDepartment == fullDepartment);

            if (orgUnit == null)
                orgUnit = AddOrgUnit(fullDepartment);
            
            // Add user to the management list for the org unit
            if (orgUnit.Management is null)
            {
                orgUnit.Management = new ApiOrgUnitManagement()
                {
                    Persons = new System.Collections.Generic.List<ApiPerson>()
                };
            }

            orgUnit.Management.Persons.Add(new ApiPerson
            {
                Name = user.Name,
                AzureUniqueId = user.AzureUniqueId.Value,
                FullDepartment = user.FullDepartment,
                Department = user.Department,
                Mail = user.Mail,
                Upn = user.Mail,
                JobTitle = user.JobTitle,
                ManagerAzureUniqueId = user.ManagerAzureUniqueId,
                AccountType = $"{user.AccountType}",
                AccountClassification = $"{user.AccountClassification}",
                MobilePhone = user.MobilePhone,
                OfficeLocation = user.OfficeLocation,
            });

            var managerRole = new ApiPersonRoleV3
            {
                Name = "Fusion.LineOrg.Manager",
                Scope = new ApiPersonRoleScopeV3 { Type = "OrgUnit", Value = orgUnit.SapId },
                IsActive = true,
                OnDemandSupport = false,
                Type = ApiFusionRoleType.Scoped,
                SourceSystem = "FusionRoleService"
            };

            if (user.Roles is null)
                user.Roles = [managerRole];
            else
                user.Roles.Add(managerRole);
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
            
            var orgUnit = LineOrgServiceMock.AddOrgUnit(user.FullDepartment);
            if (user.IsResourceOwner)
            {
                // Add user to the management list for the org unit
                if (orgUnit.Management is null)
                {
                    orgUnit.Management = new ApiOrgUnitManagement()
                    {
                        Persons = new System.Collections.Generic.List<ApiPerson>()
                    };
                }

                orgUnit.Management.Persons.Add(new ApiPerson
                {
                    Name = user.Name,
                    AzureUniqueId = user.AzureUniqueId,
                    FullDepartment = user.FullDepartment,
                    Department = user.Department,
                    Mail = user.Mail,
                    Upn = user.Mail,
                    JobTitle = user.JobTitle,
                    ManagerAzureUniqueId = user.ManagerId,
                    AccountType = "Employee", // for now..
                    AccountClassification = "Internal", // for now..
                    MobilePhone = user.Phone,
                    OfficeLocation = user.OfficeLocation,
                });
            }
            
            return user;
        }
    }

}
