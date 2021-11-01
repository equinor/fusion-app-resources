using Fusion.Resources.Database;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Testing.Mocks.ProfileService;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Integration.Profile;
using Fusion.Testing;
using System.Threading.Tasks;
using Fusion.Resources.Api.Tests.FusionMocks;
using System.Data;
using System.Linq; 
using Fusion.Events;
using Newtonsoft.Json;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourceApiFixture : IDisposable
    {
        public readonly ResourcesApiWebAppFactory ApiFactory;

        public ApiPersonProfileV3 AdminUser { get; }
        public ApiPersonProfileV3 ExternalAdminUser { get; }

        public TestClientScope ExternalAdminScope() => new TestClientScope(ExternalAdminUser);
        public TestClientScope AdminScope() => new TestClientScope(AdminUser);
        public TestClientScope UserScope(ApiPersonProfileV3 profile) => new TestClientScope(profile);

        /// <summary>
        /// Will use the admin account to delegate admin to the provided account, then returns a 'auth' scope for the delegated admin.
        /// Creats a new random user that will get the delegated role.
        /// 
        /// This new profile can be accessed through scope.Profile.
        /// </summary>
        public async Task<ApiPersonProfileV3> NewDelegatedAdminAsync(Guid projectId, Guid contractId)
        {
            var delegatedAdmin = AddProfile(FusionAccountType.External);

            var client = ApiFactory.CreateClient();

            using (var adminScope = AdminScope())
            {
                await client.DelegateExternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
            }

            return delegatedAdmin;
        }

        internal void DisableMemoryCache() => ApiFactory.isMemorycacheDisabled = true;

        public ResourceApiFixture()
        {
            ApiFactory = new ResourcesApiWebAppFactory();
            AdminUser = PeopleServiceMock.AddTestProfile()
                .WithRoles("Fusion.Resources.FullControl")
                .SaveProfile();

            ExternalAdminUser = PeopleServiceMock.AddTestProfile()
                .WithRoles("Fusion.Resources.External.FullControl")
                .SaveProfile();

        }

        public ContextResolverMock ContextResolver => ApiFactory.contextResolverMock;
        public void Dispose()
        {
            using var scope = ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
            db.Database.EnsureDeleted();
        }

        internal ApiPersonProfileV3 AddProfile(FusionAccountType accountType)
        {
            var account = PeopleServiceMock.AddTestProfile().WithAccountType(accountType).SaveProfile();
            

            return account;
        }

        internal ApiPersonProfileV3 AddResourceOwner(string department)
        {
            var resourceOwner = this.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;
            resourceOwner.FullDepartment = department;
            resourceOwner.Department = new DepartmentPath(department).GetShortName();
            LineOrgServiceMock.AddTestUser()
                .MergeWithProfile(resourceOwner)
                .WithFullDepartment(department)
                .AsResourceOwner()
                .SaveProfile();

            return resourceOwner;
        }

        internal void EnsureDepartment(string departmentId, string sectorId = null, ApiPersonProfileV3 defactoResponsible = null)
        {
            using var scope = ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

            if (defactoResponsible is not null)
            {
                db.DepartmentResponsibles.Add(new Database.Entities.DbDepartmentResponsible
                {
                    DateFrom = DateTime.Today.AddDays(-1),
                    DateTo = DateTime.Today.AddDays(1),
                    DepartmentId = departmentId,
                    ResponsibleAzureObjectId = defactoResponsible.AzureUniqueId.Value,
                });
            }

            LineOrgServiceMock.AddDepartment(departmentId);

            var resourceOwner = this.AddProfile(FusionAccountType.Application);                
            LineOrgServiceMock.AddTestUser().MergeWithProfile(resourceOwner)
                .WithFullDepartment(departmentId).SaveProfile();

            try { db.SaveChanges(); } catch (DBConcurrencyException) { }
        }

        public IReadOnlyCollection<CloudEventV1<TPayload>> GetNotificationMessages<TPayload>(string pathFilter)
        {
            var messages = TestMessageBus.GetAllMessages().Where(m => m.Path == pathFilter);

            var notifications = messages.Select(m =>
            {
                try
                {
                    return JsonConvert.DeserializeObject<CloudEventV1<TPayload>>(m.BodyText);
                }
                catch (Exception) { return null; }
            }).Where(m => m != null);


            return notifications.ToList();
        }
    }
}
