using FluentAssertions;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ContractTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private FusionTestProjectBuilder testProject = null;
        private Guid projectId => testProject.Project.ProjectId;
        private Guid contractId => testProject.ContractsWithPositions.First().Item1.Id;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public ContractTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        [Fact]
        public async Task ListContracts_ShouldDisplayContract_WhenOnlyDelegatedAdmin()
        {
            var delegatedAdmin = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                await client.DelegateExternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
            }

            using (var delegatedAdminScope = fixture.UserScope(delegatedAdmin))
            {
                var contractResp = await client.TestClientGetAsync($"/projects/{testProject.Project.ProjectId}/contracts", new { value = new[] { new { id = Guid.Empty } } });
                contractResp.Should().BeSuccessfull();

                contractResp.Value.value.Count().Should().Be(1);
            }
        }

        [Fact]
        public async Task ManagePersonnel_ShouldCreateSuccessfully_WhenExternalDelegatedAdmin()
        {
            var delegatedAdmin = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                await client.DelegateExternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
            }

            using (var delegatedAdminScope = fixture.UserScope(delegatedAdmin))
            {
                var createResp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel", new
                {
                    Mail = "someone@mail.com",
                    FirstName = "Some",
                    LastName = "Person",
                    PhoneNumber = "51515151"
                });
                createResp.Should().BeSuccessfull();

                var deleteResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/someone@mail.com");
                deleteResp.Should().BeSuccessfull();
            }
        }

        [Theory]
        [InlineData(FusionAccountType.External)]
        [InlineData(FusionAccountType.Employee)]
        [InlineData(FusionAccountType.Consultant)]
        public async Task ManagePersonnel_ShouldNOTCreateSuccessfully_WhenNoDelegation(FusionAccountType accountType)
        {
            var external = fixture.AddProfile(accountType);

            using (var delegatedAdminScope = fixture.UserScope(external))
            {
                var createResp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel", new
                {
                    Mail = "someone@mail.com",
                    FirstName = "Some",
                    LastName = "Person",
                    PhoneNumber = "51515151"
                });
                createResp.Should().BeUnauthorized();

                var deleteResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/someone@mail.com");
                deleteResp.Should().BeUnauthorized();
            }
        }

        [Fact]
        public async Task ManagePersonnel_ShouldCreateSuccessfully_WhenInternalDelegatedAdmin()
        {
            var delegatedAdmin = fixture.AddProfile(FusionAccountType.Employee);

            using (var adminScope = fixture.AdminScope())
            {
                await client.DelegateInternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
            }

            using (var delegatedAdminScope = fixture.UserScope(delegatedAdmin))
            {
                var createResp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel", new
                {
                    Mail = "someone@mail.com",
                    FirstName = "Some",
                    LastName = "Person",
                    PhoneNumber = "51515151"
                });
                createResp.Should().BeSuccessfull();

                var deleteResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/someone@mail.com");
                deleteResp.Should().BeSuccessfull();
            }
        }

        [Theory]
        [InlineData(AccountClassification.Internal)]
        [InlineData(AccountClassification.External)]
        public async Task ManagePersonnel_ShouldUpdateSuccessfully(AccountClassification classification)
        {
            var delegatedAdmin = fixture.AddProfile(classification == AccountClassification.Internal ? FusionAccountType.Employee : FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                switch (classification)
                {
                    case AccountClassification.Internal:
                        await client.DelegateInternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
                        break;
                    case AccountClassification.External:
                        await client.DelegateExternalAdminAccessAsync(projectId, contractId, delegatedAdmin.AzureUniqueId.Value);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid classification");
                };
            }

            using (var delegatedAdminScope = fixture.UserScope(delegatedAdmin))
            {
                var personnel = new
                {
                    Mail = "someone@mail.com",
                    FirstName = "Some",
                    LastName = "Person",
                    PhoneNumber = "51515151"
                };
                var createResp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel", personnel, personnel);
                createResp.Should().BeSuccessfull();

                var updateResp = await client.TestClientPutAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/someone@mail.com", createResp.Value, personnel);
                updateResp.Should().BeSuccessfull();
            }
        }

        [Fact]
        public async Task GetContractsForProject_ShouldReturnNoContracts_WhenNoneAllocated()
        {
            var testProject = new FusionTestProjectBuilder()
               .WithContractAndPositions()
               .WithContractAndPositions()
               .WithPositions()
               .AddToMockService();

            fixture.ContextResolver.AddContext(testProject.Project);

            using var adminScope = fixture.AdminScope();

            var response = await client.TestClientGetAsync($"/projects/{testProject.Project.ProjectId}/contracts", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();
        }


        #region Delete contract position
        [Fact]
        public async Task DeletePosition_ShouldBeOk_WhenOnlyDelegatedAdmin()
        {
            // Create position in test contract
            var positionToDelete = testProject.AddContractPosition(contractId);

            var delegatedAdmin = await fixture.NewDelegatedAdminAsync(projectId, contractId);

            using (var delegatedScope = fixture.UserScope(delegatedAdmin))
            {
                var contractResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/mpp/positions/{positionToDelete.Id}");
                contractResp.Should().BeSuccessfull();

                // Check that the position has been deleted from the org service
                var resolver = fixture.ApiFactory.Services.GetRequiredService<IProjectOrgResolver>();
                var positionCheck = await resolver.ResolvePositionAsync(positionToDelete.Id);
                positionCheck.Should().BeNull();
            }
        }

        [Fact]
        public async Task DeletePosition_ShouldBeAllowed_WhenOnlyDelegatedAdmin()
        {
            var delegatedAdmin = await fixture.NewDelegatedAdminAsync(projectId, contractId);

            using (var delegatedScope = fixture.UserScope(delegatedAdmin))
            {
                var contractResp = await client.TestClientOptionsAsync($"/projects/{projectId}/contracts/{contractId}/mpp/positions");
                contractResp.Should().HaveAllowHeaders(HttpMethod.Delete);
            }
        }

        [Fact]
        public async Task DeletePosition_ShouldBeBadRequest_WhenPositionDoesNotExist()
        {
            using (var authScope = fixture.AdminScope())
            {
                var contractResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/mpp/positions/{Guid.NewGuid()}");
                contractResp.Should().BeBadRequest();
            }
        }

        [Fact]
        public async Task DeletePosition_ShouldBeAccessDenied_WhenRandomUser()
        {
            // Create position in test contract
            var positionToDelete = testProject.AddContractPosition(contractId);

            using (var userScope = fixture.UserScope(testUser))
            {
                var contractResp = await client.TestClientDeleteAsync($"/projects/{projectId}/contracts/{contractId}/mpp/positions/{positionToDelete.Id}");
                contractResp.Should().BeUnauthorized();

                // Ensure position still exists
                var resolver = fixture.ApiFactory.Services.GetRequiredService<IProjectOrgResolver>();
                var positionCheck = await resolver.ResolvePositionAsync(positionToDelete.Id);
                positionCheck.Should().NotBeNull();
            }
        }
        #endregion


        [Fact]
        public async Task AllocateContract()
        {
            using var adminScope = fixture.AdminScope();

            var response = await client.TestClientPostAsync($"/projects/{testProject.Project.ProjectId}/contracts", new
            {
                ContractNumber = "12345",
                Name = $"New contract {Guid.NewGuid()}"
            });
            response.Should().BeSuccessfull();
        }

        /*[Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]*/
        public async Task RecertifyRoleDelegation(int offset, bool expectingSuccess)
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/delegated-roles", new
            {
                person = new { AzureUniquePersonId = testUser.AzureUniqueId }, classification = "External", type = "CR"
            }, new { Id = Guid.Empty });
            resp.Response.EnsureSuccessStatusCode();

            var roleId = resp.Value.Id;

            var response = await client.TestClientPatchAsync<object>($"/projects/{testProject.Project.ProjectId}/contracts/{contractId}/delegated-roles/{roleId}", new
            {
                ValidTo = DateTimeOffset.UtcNow.AddMonths(offset)
            });
            if (expectingSuccess)
                response.Should().BeSuccessfull();
            else
                response.Should().BeBadRequest();
        }

        #region Poc tests

        [Fact]
        public async Task ContextResolver()
        {
            var testProject = new FusionTestProjectBuilder();

            var contextResolver = new ContextResolverMock()
                .AddContext(testProject.Project);

            var context = await contextResolver.QueryContextsAsync(q => q.WhereExternalId($"{testProject.Project.ProjectId}", QueryOperator.Equals));
            context.Should().Contain(c => c.ExternalId == $"{testProject.Project.ProjectId}");
        }


        [Fact]
        public async Task PeopleMock()
        {

            var testProfile = PeopleServiceMock.AddTestProfile()
                .SaveProfile();


            var pplService = new PeopleServiceMock()
                .CreateHttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"/persons/{testProfile.AzureUniqueId}");
            request.Headers.Add("api-version", "3.0");
            var resp = await pplService.SendAsync(request);

            var content = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

        }

        [Fact]
        public async Task OrgMock()
        {

            var testProject = new FusionTestProjectBuilder()
                .WithContractAndPositions()
                .WithPositions()
                .AddToMockService();


            var orgService = new OrgServiceMock()
                .CreateHttpClient();


            var resp = await orgService.GetAsync($"/projects/{testProject.Project.ProjectId}");
            var content = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

        }

        #endregion


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
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
