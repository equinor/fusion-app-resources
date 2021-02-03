using Fusion.Resources.Database;
using System;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Testing.Mocks.ProfileService;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Integration.Profile;
using Fusion.Testing;
using System.Threading.Tasks;
using Fusion.Resources.Api.Tests.FusionMocks;

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
    }
}
