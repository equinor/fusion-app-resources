using Fusion.Resources.Database;
using System;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Testing.Mocks.ProfileService;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Integration.Profile;
using Fusion.Testing;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourceApiFixture : IDisposable
    {
        public readonly ResourcesApiWebAppFactory ApiFactory;

        public ApiPersonProfileV3 AdminUser { get; }

        public TestClientScope AdminScope() => new TestClientScope(AdminUser);
        public TestClientScope UserScope(ApiPersonProfileV3 profile) => new TestClientScope(profile);


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
