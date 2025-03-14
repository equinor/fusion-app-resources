using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ProfileService.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;
using HashLib;
using HashLib.Checksum;

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
        public FusionTestUserBuilder WithPreferredContactMail(string mail)
        {
            profile.PreferredContactMail = mail;
            return this;
        }

        /// <summary>
        /// Will flag the profile as resource owner by setting the bool property to true. 
        /// The profile will also be added to the department above or CEC.
        /// 
        /// A LineOrg role will be added as well.
        /// 
        /// Should be called after the full department has been updated.
        /// </summary>
        /// <returns></returns>
        public FusionTestUserBuilder AsResourceOwner()
        {
            profile.IsResourceOwner = true;

            try
            {
                var depTokens = profile.FullDepartment.Split(' ');
                // Lift manager 1 level up. 

                var parentFullDepartment = string.Join(" ", depTokens.SkipLast(1));
                var parentDepartment = string.Join(" ", depTokens.SkipLast(1).TakeLast(3));

                if (string.IsNullOrEmpty(parentDepartment))
                    parentDepartment = "CEC";
                if (string.IsNullOrEmpty(parentFullDepartment))
                    parentFullDepartment = "CEC";
                
                profile.FullDepartment = parentFullDepartment;
                profile.Department = parentDepartment;

            }
            catch (Exception) {  /* */ }

            // Must add roles.. Create SAP id
            if (profile.Roles is null)
                profile.Roles = new List<ApiPersonRoleV3>();

            // Generate sapId same way as lineorg mock
            var hash = HashFactory.Checksum.CreateCRC32(0xF0F0F0F0);
            var hashResult = hash.ComputeString(profile.FullDepartment);
            var sapId = $"{Math.Abs(hashResult.GetInt())}";


            profile.Roles = new List<ApiPersonRoleV3>
                {
                    new ApiPersonRoleV3
                    {
                        Name = "Fusion.LineOrg.Manager",
                        Scope = new ApiPersonRoleScopeV3 { Type = "OrgUnit", Value = sapId },
                        IsActive = true,
                        OnDemandSupport = false
                    }
                };

            return this;
        }
        public FusionTestUserBuilder WithAccountType(FusionAccountType type)
        {
            profile.AccountType = type;
            profile.AccountClassification = type == FusionAccountType.Employee ? AccountClassification.Internal : AccountClassification.External;
            return this;
        }


        public FusionTestUserBuilder WithManager(Guid? azureUniqueId)
        {
            profile.ManagerAzureUniqueId = azureUniqueId;
            return this;
        }
        public FusionTestUserBuilder WithManager(ApiPersonProfileV3 person)
        {
            profile.ManagerAzureUniqueId = person.AzureUniqueId;
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

        public FusionTestUserBuilder WithPositions(IEnumerable<ApiPositionV2> positions)
        {
            foreach (var position in positions)
                WithPosition(position);
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
