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

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourceApiFixture : IDisposable
    {
        public readonly ResourcesApiWebAppFactory ApiFactory;

        public ApiPersonProfileV3 AdminUser { get; }

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

        public ResourceApiFixture()
        {
            ApiFactory = new ResourcesApiWebAppFactory();
            AdminUser = PeopleServiceMock.AddTestProfile()
                .WithRoles("Fusion.Resources.FullControl")
                .SaveProfile();

        }

        public ContextResolverMock ContextResolver => ApiFactory.contextResolverMock;
        public MockHttpClientBuilder LineOrg => ApiFactory.lineOrgMock;

        public void Dispose()
        {
            using var scope = ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
            db.Database.EnsureDeleted();
        }

        internal ApiPersonProfileV3 AddProfile(FusionAccountType accountType)
        {
            var account = new FusionTestUserBuilder(accountType)
                .SaveProfile();


            return account;
        }
        internal void EnsureDepartment(string departmentId, string sectorId = null, ApiPersonProfileV3 defactoResponsible = null)
        {
            using var scope = ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

            var dept = db.Departments.Find(departmentId) ?? new Database.Entities.DbDepartment();
            var entry = db.Entry(dept);

            dept.DepartmentId = departmentId;
            dept.SectorId = sectorId;

            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }

            if(defactoResponsible is not null)
            {
                db.DepartmentResponsibles.Add(new Database.Entities.DbDepartmentResponsible
                {
                    DateFrom = DateTime.Today.AddDays(-1),
                    DateTo = DateTime.Today.AddDays(1),
                    DepartmentId = departmentId,
                    ResponsibleAzureObjectId = defactoResponsible.AzureUniqueId.Value,
                });
            }

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
