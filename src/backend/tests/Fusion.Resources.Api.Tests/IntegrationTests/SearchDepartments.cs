using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Testing.Mocks.LineOrgService;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests
{
    [Collection("Integration")]
    public class SearchDepartments : IClassFixture<ResourceApiFixture>
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 testUser;

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public SearchDepartments(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
        }

        //PLEASEFIX[Fact]
        public async Task ShouldGetDataFromLineOrg()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            fixture.EnsureDepartment("TPD PRD FE MMS STR2", "TPD PRD FE MMS");
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().WithFullDepartment("TPD PRD FE MMS STR2").SaveProfile();

            using var authScope = fixture.AdminScope();

            var search = fakeResourceOwner.Name.Substring(0, Math.Min(4, fakeResourceOwner.Name.Length));
            var result = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments&$search={search}");
            result.Value.Single().LineOrgResponsible.AzureUniquePersonId.Should().Be(fakeResourceOwner.AzureUniqueId.Value);
        }

        //PLEASEFIX[Fact]
        public async Task ShouldIncludeDefactoResourceOwner()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var fakeDefactoResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            fixture.EnsureDepartment("TPD PRD FE MMS STR2", "TPD PRD FE MMS", fakeDefactoResourceOwner);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).WithFullDepartment("TPD PRD FE MMS STR2").SaveProfile();
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeDefactoResourceOwner).AsResourceOwner().WithFullDepartment("TPD PRD FE MMS STR2").SaveProfile();

            using var authScope = fixture.AdminScope();

            var search = fakeResourceOwner.Name.Substring(0, Math.Min(4, fakeResourceOwner.Name.Length));
            var result = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments&$search={search}");
            result.Value.SingleOrDefault()?.LineOrgResponsible.AzureUniquePersonId.Should().Be(fakeResourceOwner.AzureUniqueId.Value);

            var delegatedResponsible = result.Value.SingleOrDefault()?.DelegatedResponsibles.SingleOrDefault();
            delegatedResponsible.Should().NotBeNull();
            delegatedResponsible.AzureUniquePersonId.Should().Be(fakeDefactoResourceOwner.AzureUniqueId.Value);
        }
    }

    public class TestDepartment
    {
        public string Name { get; set; }
        public string Sector { get; set; }
        public TestApiPerson LineOrgResponsible { get; set; }
        public List<TestApiPerson> DelegatedResponsibles { get; set; }
    }
}