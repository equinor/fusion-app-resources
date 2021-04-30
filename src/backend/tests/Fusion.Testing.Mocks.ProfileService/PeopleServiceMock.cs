using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ProfileService.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Fusion.Testing.Mocks.ProfileService
{
    public class PeopleServiceMock
    {
        readonly WebApplicationFactory<Startup> factory;

        internal static List<ApiPersonProfileV3> profiles = new List<ApiPersonProfileV3>();
        internal static List<ApiCompanyInfo> companies = new List<ApiCompanyInfo>();

        public PeopleServiceMock()
        {
            factory = new WebApplicationFactory<Startup>();
        }

        public HttpClient CreateHttpClient()
        {
            var client = factory.CreateClient();
            return client;
        }

        public static FusionTestUserBuilder AddTestProfile() => new FusionTestUserBuilder();
        public static void AddCompany(Guid id, string name) => companies.Add(new ApiCompanyInfo { Id = id, Name = name });
    }

    public class FusionTestUserBuilder
    {
        private readonly ApiPersonProfileV3 profile;
        public FusionTestUserBuilder()
        {
            profile = FusionTestProfiles.CreateTestUser();
        }

        public FusionTestUserBuilder(FusionAccountType type, AccountClassification? classification = null)
        {
            profile = FusionTestProfiles.CreateTestUser(type, classification);
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
