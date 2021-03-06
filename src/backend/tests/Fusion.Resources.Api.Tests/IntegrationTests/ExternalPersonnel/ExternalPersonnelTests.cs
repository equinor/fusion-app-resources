﻿using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests.ExternalPersonnel
{
    public class ExternalPersonnelTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private readonly ApiPersonProfileV3 testUser;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        private FusionTestProjectBuilder testProject = null;
        private Guid projectId => testProject.Project.ProjectId;
        private Guid contractId => testProject.ContractsWithPositions.First().Item1.Id;


        public ExternalPersonnelTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        [Fact]
        public async Task PreferredMail_Options_ShouldBeBadRequest_WhenPrivateMailProvider()
        {
            var invalidMails = new[]
            {
                "mail@mail.com",
                "someone@hotmail.com",
                "test@icloud.com",
                "test@gmail.com",
                "not-even-a-mail"
            };

            // No extra authentication on validation endpoint.
            using var scope = fixture.UserScope(testUser);

            foreach (var invalidMail in invalidMails)
            {
                var resp = await client.TestClientOptionsAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/preferred-contact?mail={invalidMail}");
                resp.Should().BeBadRequest();
            }
        }
        [Fact]
        public async Task PreferredMail_Options_ShouldBeOk_WhenCompanyMail()
        {
            var validMails = new[]
            {
                "mail@my-company.com"
            };

            // No extra authentication on validation endpoint.
            using var scope = fixture.UserScope(testUser);

            foreach (var mail in validMails)
            {
                var resp = await client.TestClientOptionsAsync($"/projects/{projectId}/contracts/{contractId}/resources/personnel/preferred-contact?mail={mail}");
                resp.Should().BeSuccessfull();
            }
        }

        [Fact]
        public async Task PreferredMail_UpdatePersonnel_ShouldTriggerUpdateInPeopleApi_WhenChanged()
        {
            var testPersonnel = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                var person = await client.CreatePersonnelAsync(projectId, contractId, s => { s.Mail = testPersonnel.Mail; });

                person.PreferredContactMail = "mail@other-company.com";

                var resp = await client.TestClientPutAsync<TestApiPersonnel>($"/projects/{projectId}/contracts/{contractId}/resources/personnel/{person.PersonnelId}", person);
                resp.Should().BeSuccessfull();

                PeopleIntegrationMock.Requests.Should().Contain(r => r.azureId == testPersonnel.AzureUniqueId.Value);
            }
        }


        [Fact]
        public async Task PreferredMail_UpdatePersonnel_ShouldNotTriggerUpdateInPeopleApi_WhenNotChanged()
        {
            var testPersonnel = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                var person = await client.CreatePersonnelAsync(projectId, contractId, s => { s.Mail = testPersonnel.Mail; });

                person.PreferredContactMail = null;

                var resp = await client.TestClientPutAsync<TestApiPersonnel>($"/projects/{projectId}/contracts/{contractId}/resources/personnel/{person.PersonnelId}", person);
                resp.Should().BeSuccessfull();

                PeopleIntegrationMock.Requests.Should().NotContain(r => r.azureId == testPersonnel.AzureUniqueId.Value);
            }
        }

        [Fact]
        public async Task PreferredMail_UpdatePersonnel_ShouldBePersistedLocally()
        {
            var testPersonnel = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                var person = await client.CreatePersonnelAsync(projectId, contractId, s => { s.Mail = testPersonnel.Mail; });
                person.PreferredContactMail = "mail@other-company.com";

                var resp = await client.TestClientPutAsync<TestApiPersonnel>($"/projects/{projectId}/contracts/{contractId}/resources/personnel/{person.PersonnelId}", person);
                resp.Should().BeSuccessfull();

                resp.Value.PreferredContactMail.Should().Be(person.PreferredContactMail);
            }
        }

        [Fact]
        public async Task PreferredMail_UpdateBatchPersonnel_ShouldBePersistedLocally()
        {
            // Must add actual accounts, as they must exist in ad to be able to update
            var testPersonnelA = fixture.AddProfile(FusionAccountType.External);
            var testPersonnelB = fixture.AddProfile(FusionAccountType.External);

            using (var adminScope = fixture.AdminScope())
            {
                var personA = await client.CreatePersonnelAsync(projectId, contractId, s => { s.Mail = testPersonnelA.Mail; });
                var personB = await client.CreatePersonnelAsync(projectId, contractId, s => { s.Mail = testPersonnelB.Mail; });
                
                var resp = await client.TestClientPutAsync<ApiCollection<TestApiPersonnel>>($"/projects/{projectId}/contracts/{contractId}/resources/personnel/preferred-contact", new
                {
                    personnel = new []
                    {
                        new { personnelId = personA.PersonnelId, preferredContactMail = "some-a@other-company.com" }, 
                        new { personnelId = personB.PersonnelId, preferredContactMail = "some-b@other-company.com" } 
                    }
                });
                resp.Should().BeSuccessfull();

                resp.Value.Value.Should().Contain(i => i.PersonnelId == personA.PersonnelId && i.PreferredContactMail == "some-a@other-company.com");
                resp.Value.Value.Should().Contain(i => i.PersonnelId == personB.PersonnelId && i.PreferredContactMail == "some-b@other-company.com");

                resp.Value.Value.Should().OnlyContain(i => i.PersonnelId == personA.PersonnelId || i.PersonnelId == personB.PersonnelId);
            }
        }

        [Fact]
        public async Task PreferredMail_UpdateBatchPersonnel_ShouldValidateEmail()
        {
            using (var adminScope = fixture.AdminScope())
            {
                var person = await client.CreatePersonnelAsync(projectId, contractId);

                var resp = await client.TestClientPutAsync<ApiCollection<TestApiPersonnel>>($"/projects/{projectId}/contracts/{contractId}/resources/personnel/preferred-contact", new
                {
                    personnel = new[]
                    {
                        new { personnelId = person.PersonnelId, preferredContactMail = "invalid@gmail.com" }
                    }
                });
                resp.Should().BeBadRequest();
            }
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

            // Make the contract created on the project available (allocate)
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

            response.EnsureSuccessStatusCode();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;    
        }
    }
}
