using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ContractPersonnelReplacementTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUserA1Expired;
        private readonly ApiPersonProfileV3 testUserA1;
        private readonly ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private FusionTestProjectBuilder testProject = null;
        private Guid projectId => testProject.Project.ProjectId;
        private Guid contractId => testProject.ContractsWithPositions.First().Item1.Id;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public ContractPersonnelReplacementTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUserA1Expired = PeopleServiceMock.AddTestProfile()
                .WithAccountType(FusionAccountType.External).SaveProfile();

            testUserA1 = PeopleServiceMock.AddTestProfile()
                .WithAccountType(FusionAccountType.External)
                .WithUpn(testUserA1Expired.UPN).SaveProfile();

            testUser = fixture.AddProfile(FusionAccountType.External);

        }

        [Fact]
        public async Task ReplacePersonnel_ShouldUpdateSuccessfully_WhenUpnMatch()
        {

            using var adminScope = fixture.AdminScope();

            var resp = await client.ReplaceContractPersonnelAsync(projectId, contractId, testUserA1Expired.AzureUniqueId.Value, testUserA1.UPN, testUserA1.AzureUniqueId.Value);
            resp.Should().BeSuccessfull();
        }
        [Fact]
        public async Task ReplacePersonnel_ShouldUpdateSuccessfully_WhenUpnMisMatchAndForced()
        {

            using var adminScope = fixture.AdminScope();

            var resp = await client.ReplaceContractPersonnelAsync(projectId, contractId, testUserA1Expired.AzureUniqueId.Value, testUser.UPN, testUser.AzureUniqueId.Value, true);
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task ReplacePersonnel_ShouldFail_WhenUpnMismatch()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.ReplaceContractPersonnelAsync(projectId, contractId, testUserA1Expired.AzureUniqueId.Value, testUser.UPN, testUser.AzureUniqueId.Value);
            resp.Should().BeBadRequest();
        }

        [Fact]
        public async Task ReplacePersonnel_ShouldFail_WhenInvalidAzureUniqueIdArgument()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.ReplaceContractPersonnelAsync(projectId, contractId, testUserA1Expired.AzureUniqueId.Value, testUser.UPN, Guid.Empty);
            resp.Should().BeBadRequest();
        }

        [Fact]
        public async Task ReplacePersonnel_ShouldFail_WhenInvalidUpnArgument()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.ReplaceContractPersonnelAsync(projectId, contractId, testUserA1Expired.AzureUniqueId.Value, string.Empty, testUser.AzureUniqueId.Value);
            resp.Should().BeBadRequest();
        }

        public async Task InitializeAsync()
        {

            testProject = new FusionTestProjectBuilder()
               .WithContractAndPositions()
               .WithPositions()
               .AddToMockService();

            fixture.ContextResolver
                .AddContext(testProject.Project);

            var client = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            (var contract, var positions) = testProject.ContractsWithPositions.First();



            // Make the company available in the ppl service
            if (contract.Company != null)
                Testing.Mocks.ProfileService.PeopleServiceMock.AddCompany(contract.Company.Id, contract.Company.Name);

            var response = await client.PostAsJsonAsync($"/projects/{testProject.Project.ProjectId}/contracts", new
            {
                ContractNumber = contract.ContractNumber,
                Name = contract.Name,
                Description = contract.Description,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Company = new { id = contract.Company.Id },
                CompanyRepPositionId = testProject.Positions.Skip(1).First().Id,
                ExternalCompanyRepPositionId = positions.First().Id
            });

            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            // To make sure all test users are available for contract personnel, and testUserA1 is marked as deleted
            await EnsureContractPersonAsync(testUser);
            await EnsureContractPersonAsync(testUserA1);
            await EnsureContractPersonAsync(testUserA1Expired);
            await EnsureProfileMarkedAsDeletedInDatabaseAsync(testUserA1Expired);

        }
        private async Task EnsureContractPersonAsync(ApiPersonProfileV3 profile)
        {
            using var adminScope = fixture.AdminScope();
            var createResp = await client.TestClientPostAsync(
                $"/projects/{projectId}/contracts/{contractId}/resources/personnel", new
                {
                    Mail = profile.Mail,
                    FirstName = profile.Name,
                    LastName = profile.Name,
                    PhoneNumber = profile.MobilePhone
                });
            createResp.Should().BeSuccessfull();
        }

        private async Task EnsureProfileMarkedAsDeletedInDatabaseAsync(ApiPersonProfileV3 profile)
        {
            using var scope = fixture.ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

            var dbPerson = await db.ExternalPersonnel.FirstOrDefaultAsync(x => x.Mail == profile.Mail);
            if (dbPerson is not null)
            {
                dbPerson.IsDeleted = true;
                dbPerson.Deleted = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync();
            }
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
