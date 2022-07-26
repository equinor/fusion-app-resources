using FluentAssertions;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Test;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Fusion.Resources.Logic.Commands.ContractorPersonnelRequest;

namespace Fusion.Resources.Logic.Tests
{
    public class CheckUserAccessCommandTests
    {
        private readonly Guid userAzureUniqueId;
        private readonly Guid projectId;
        private readonly Guid contractId;

        public CheckUserAccessCommandTests()
        {
            userAzureUniqueId = Guid.NewGuid();
            projectId = Guid.NewGuid();
            contractId = Guid.NewGuid();
        }

        [Fact]
        public async Task DelegatedExternalRole_Should_BeApprover_When_Request_State_Is_Created()
        {
            var contract = ApiContractBuilder.NewContract(contractId)
                .WithCompanyRep(userAzureUniqueId);

            var orgResolverMock = new Mock<IProjectOrgResolver>();
            orgResolverMock.Setup(r => r.ResolveContractAsync(projectId, contractId)).ReturnsAsync(contract);

            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(CheckUserAccessCommandTests))
                .Options;

            var dbContext = new ResourcesDbContext(dbOptions);

            var request = new DbContractorRequest
            {
                Id = Guid.NewGuid(),
                ContractId = contractId,
                Contract = new DbContract { OrgContractId = contractId, ContractNumber = "1234", Name = "Test Contract" },
                Created = DateTime.Now,
                CreatedById = userAzureUniqueId,
                Project = new DbProject { OrgProjectId = projectId, Name = "Test project" },
                ProjectId = projectId,
                State = DbRequestState.Created,
                Category = DbRequestCategory.NewRequest,
                Position = new DbContractorRequest.RequestPosition { Name = "Test position" }
            };

            dbContext.Add(request);

            var delegatedRole = new DbDelegatedRole
            {
                Id = Guid.NewGuid(),
                Classification = DbDelegatedRoleClassification.External,
                Contract = request.Contract,
                Created = DateTimeOffset.UtcNow,
                CreatedById = userAzureUniqueId,
                Person = new DbPerson { AzureUniqueId = userAzureUniqueId, Name = "Testas Testesen", AccountType = "Employee" },
                ValidTo = DateTimeOffset.UtcNow.AddDays(30)
            };

            dbContext.Add(delegatedRole);
            await dbContext.SaveChangesAsync();

            var handler = new CheckUserAccess.Handler(dbContext, orgResolverMock.Object);
            var authRequest = new CheckUserAccess(request.Id);
            authRequest.SetEditor(userAzureUniqueId, null);

            var response = await handler.Handle(authRequest, new CancellationToken());

            response.Should().BeTrue();
        }
    }
}
