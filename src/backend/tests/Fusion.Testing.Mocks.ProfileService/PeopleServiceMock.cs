using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ProfileService.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Fusion.Testing.Mocks.ProfileService
{
    public class PeopleServiceMock
    {
        readonly WebApplicationFactory<Startup> factory;

        internal static ConcurrentBag<ApiPersonProfileV3> profiles = new ConcurrentBag<ApiPersonProfileV3>();
        internal static ConcurrentBag<ApiCompanyInfo> companies = new ConcurrentBag<ApiCompanyInfo>();

        public PeopleServiceMock()
        {
            factory = new WebApplicationFactory<Startup>();
        }

        public HttpClient CreateHttpClient()
        {
            var client = factory.CreateClient();
            return client;
        }

        public static FusionTestUserBuilder AddTestProfile() => new();
        public static void AddCompany(Guid id, string name) => companies.Add(new ApiCompanyInfo { Id = id, Name = name });
    }

    public class FusionTestUserBuilder
    {
        private readonly ApiPersonProfileV3 profile;
        public FusionTestUserBuilder()
        {
            profile = FusionTestProfiles.CreateTestUser();
            profile.Positions = new List<ApiPersonPositionV3>();
        }

        public FusionTestUserBuilder(FusionAccountType type, AccountClassification? classification = null)
        {
            profile = FusionTestProfiles.CreateTestUser(type, classification);
        }

        public FusionTestUserBuilder WithUpn(string upn)
        {
            profile.UPN = upn;
            return this;
        }

        public FusionTestUserBuilder WithAccountType(FusionAccountType type)
        {
            profile.AccountType = type;
            profile.AccountClassification = type == FusionAccountType.Employee ? AccountClassification.Internal : AccountClassification.External;
            return this;
        }

        public FusionTestUserBuilder WithFullDepartment(string fullDepartment)
        {
            profile.FullDepartment = fullDepartment;
            return this;
        }
        public FusionTestUserBuilder WithDepartment(string department)
        {
            profile.Department = department;
            return this;
        }

        public FusionTestUserBuilder WithPosition(ApiPositionV2 position)
        {
            position.Instances.ForEach(instance => profile.Positions.Add(new ApiPersonPositionV3
            {
                AppliesFrom = instance.AppliesFrom,
                AppliesTo = instance.AppliesTo,
                ParentPositionId = position.Id,
                PositionExternalId = position.ExternalId,
                BasePosition = new ApiPersonBasePositionV3
                               {
                                   Id = position.BasePosition.Id,
                                   Name = position.BasePosition.Name,
                                   Discipline = position.BasePosition.Discipline,
                                   SubDiscipline = position.BasePosition.SubDiscipline,
                                   Type = position.BasePosition.ProjectType
                               },
                Project = new ApiPersonPositionProjectV3
                          {
                              DomainId = position.Project.DomainId,
                              Id = position.Project.ProjectId,
                              Name = position.Project.Name,
                              Type = position.Project.Name,
                          },
                Workload = instance.Workload,
                Obs = instance.Obs,
                Name = position.Name,
                Id = instance.Id,
                PositionId = position.Id
            }));

            return this;
        }

        public FusionTestUserBuilder WithRoles(string name)
        {
            if (profile.Roles == null) profile.Roles = new List<ApiPersonRoleV3>();

            profile.Roles.Add(new ApiPersonRoleV3()
            {
                Name = name,
                IsActive = true,
                SourceSystem = "FusionTesting",
                Type = ApiFusionRoleType.Global
            });
            return this;
        }

        public FusionTestUserBuilder WithRoles(string name, ApiFusionRoleScopeType scopeType, Guid scopeValue) => WithRoles(name, scopeType, $"{scopeValue}");
        public FusionTestUserBuilder WithRoles(string name, ApiFusionRoleScopeType scopeType, string scopeValue)
        {
            if (profile.Roles == null) profile.Roles = new List<ApiPersonRoleV3>();

            profile.Roles.Add(new ApiPersonRoleV3()
            {
                Name = name,
                SourceSystem = "FusionTesting",
                Type = ApiFusionRoleType.Scoped,
                Scope = new ApiPersonRoleScopeV3()
                {
                    Type = $"{scopeType}",
                    Value = scopeValue
                }
            });
            return this;
        }

        public ApiPersonProfileV3 SaveProfile()
        {
            PeopleServiceMock.profiles.Add(profile);
            return profile;
        }
    }

}
