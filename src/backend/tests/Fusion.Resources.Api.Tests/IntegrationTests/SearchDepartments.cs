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
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests
{
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

        [Fact]
        public async Task ShouldGetDataFromLineOrg()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD PRD FE MMS STR2"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);
            fixture.EnsureDepartment("TPD PRD FE MMS STR2", "TPD PRD FE MMS");

            using var authScope = fixture.AdminScope();

            var search = fakeResourceOwner.Name.Substring(0, Math.Min(4, fakeResourceOwner.Name.Length));
            var result = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?api-version=1.0-preview&$search={search}");
            result.Value.Single().LineOrgResponsible.AzureUniquePersonId.Should().Be(fakeResourceOwner.AzureUniqueId.Value);
        }

        [Fact]
        public async Task ShouldIncludeDefactoResourceOwner()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var fakeDefactoResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD PRD FE MMS STR2"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);
            fixture.EnsureDepartment("TPD PRD FE MMS STR2", "TPD PRD FE MMS", fakeDefactoResourceOwner);

            using var authScope = fixture.AdminScope();
            
            var search = fakeResourceOwner.Name.Substring(0, Math.Min(4, fakeResourceOwner.Name.Length));
            var result = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?api-version=1.0-preview&$search={search}");
            result.Value.Single().LineOrgResponsible.AzureUniquePersonId.Should().Be(fakeResourceOwner.AzureUniqueId.Value);

            var delegatedResponsible = result.Value.Single().DelegatedResponsibles.Single();
            delegatedResponsible.Should().NotBeNull();
            delegatedResponsible.AzureUniquePersonId.Should().Be(fakeDefactoResourceOwner.AzureUniqueId.Value);
        }
    }

    public class TestDepartment
    {
        public string Name { get; set; }
        public string? Sector { get; set; }
        public TestApiPerson LineOrgResponsible { get; set; }
        public List<TestApiPerson>? DelegatedResponsibles { get; set; }
    }
}
