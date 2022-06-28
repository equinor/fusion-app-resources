using Bogus;
using FluentAssertions;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Api.Authorization.Handlers;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Api.Tests
{
    public class DelegatedRoleHandlerTests : AuthorizationHandlerTestBase, IDisposable
    {
        private readonly Guid userAzureUniqueId;
        private readonly Guid projectId;
        private readonly Guid contractId;
        private readonly string projectName;
        private readonly Controllers.PathProjectIdentifier project;
        private readonly DelegatedContractRoleAuthHandler handler;
        private readonly ContractResource resource;

        private readonly ResourcesDbContext dbContext;
        private DbPerson testPerson = null;
        private DbProject testProject = null;
        private DbContract testContract = null;


        public DelegatedRoleHandlerTests()
        {
            userAzureUniqueId = Guid.NewGuid();
            projectId = Guid.NewGuid();
            contractId = Guid.NewGuid();
            projectName = $"Test project {projectId}";
            project = new Controllers.PathProjectIdentifier($"{projectId}", projectId, projectName);
            resource = new ContractResource(project, contractId);

            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase("DelegatedRoleHandlerTests")
                .Options;

            dbContext = new ResourcesDbContext(dbOptions);
            handler = new DelegatedContractRoleAuthHandler(dbContext);

            InitTestDatabase();
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep)]
        public async Task AnyExternal_Should_Succeed_When_UserHasExternalRole_CR(DelegatedContractRole.RoleType role)
        {
            var requirement = DelegatedContractRole.AnyExternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(DelegatedContractRole.RoleClassification.External, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep)]
        public async Task AnyInternal_Should_Succeed_When_UserHasInternalRole(DelegatedContractRole.RoleType role)
        {
            var requirement = DelegatedContractRole.AnyInternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(DelegatedContractRole.RoleClassification.Internal, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CR_Internal_Should_Succeed_When_UserHasInternalRole_CR()
        {
            var requirement = new DelegatedContractRole(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal);
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(DelegatedContractRole.RoleClassification.Internal, DelegatedContractRole.RoleType.CompanyRep);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.External)]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal)]
        public async Task SpecificRole_Should_Succeed_WhenUserHasRole(DelegatedContractRole.RoleType role, DelegatedContractRole.RoleClassification classification)
        {
            var requirement = new DelegatedContractRole(role, classification);
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(classification, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }


        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.External, false)]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal, true)]
        public async Task AnyInternal_Evaluation(DelegatedContractRole.RoleType role, DelegatedContractRole.RoleClassification classification, bool shouldSucceed)
        {
            var requirement = DelegatedContractRole.AnyInternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(classification, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().Be(shouldSucceed);
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.External, true)]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal, false)]
        public async Task AnyExternal_Evaluation(DelegatedContractRole.RoleType role, DelegatedContractRole.RoleClassification classification, bool shouldSucceed)
        {
            var requirement = DelegatedContractRole.AnyExternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(classification, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().Be(shouldSucceed);
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.External, true)]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal, true)]
        public async Task Any_Evaluation(DelegatedContractRole.RoleType role, DelegatedContractRole.RoleClassification classification, bool shouldSucceed)
        {
            var requirement = DelegatedContractRole.Any;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(classification, role);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().Be(shouldSucceed);
        }

        [Theory]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.External)]
        [InlineData(DelegatedContractRole.RoleType.CompanyRep, DelegatedContractRole.RoleClassification.Internal)]
        public async Task Any_ShouldFail_WhenDelegationIsExpired(DelegatedContractRole.RoleType role, DelegatedContractRole.RoleClassification classification)
        {
            var requirement = DelegatedContractRole.Any;
            var user = GetClaimsUser(userAzureUniqueId);

            AddUserRole(classification, role, TimeSpan.FromDays(-10));

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AnyInternal_ShouldFail_When_NoRoles()
        {
            var requirement = DelegatedContractRole.AnyInternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AnyExternal_ShouldFail_When_NoRoles()
        {
            var requirement = DelegatedContractRole.AnyExternalRole;
            var user = GetClaimsUser(userAzureUniqueId);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Any_ShouldFail_When_NoRoles()
        {
            var requirement = DelegatedContractRole.Any;
            var user = GetClaimsUser(userAzureUniqueId);

            var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }





        private void AddUserRole(DelegatedContractRole.RoleClassification classification, DelegatedContractRole.RoleType type, TimeSpan? validTo = null)
        {

            var role = new DbDelegatedRole
            {
                PersonId = testPerson.Id,
                ProjectId = testProject.Id,
                ContractId = testContract.Id,
                ValidTo = DateTimeOffset.UtcNow.Add(validTo.GetValueOrDefault(TimeSpan.FromDays(10))),
                Classification = classification switch
                {
                    DelegatedContractRole.RoleClassification.External => DbDelegatedRoleClassification.External,
                    DelegatedContractRole.RoleClassification.Internal => DbDelegatedRoleClassification.Internal,
                    _ => throw new NotSupportedException($"{classification} not supported")
                },
                Type = type switch
                {
                    DelegatedContractRole.RoleType.CompanyRep => DbDelegatedRoleType.CR,
                    _ => throw new InvalidOperationException($"Invalid role type or role type not supported")
                }
            };

            dbContext.DelegatedRoles.Add(role);
            dbContext.SaveChanges();
        }

        private void InitTestDatabase()
        {
            testPerson = new Faker<DbPerson>()
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.AzureUniqueId, f => userAzureUniqueId)
                .RuleFor(p => p.AccountType, _ => "Employee")
                .Generate();

            testProject = new Faker<DbProject>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Lorem.Sentence())
                .RuleFor(p => p.OrgProjectId, f => projectId)
                .Generate();

            testContract = new Faker<DbContract>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Lorem.Sentence())
                .RuleFor(p => p.OrgContractId, f => contractId)
                .RuleFor(p => p.ProjectId, f => testProject.Id)
                .RuleFor(x => x.ContractNumber, f => f.Random.Number().ToString("000000"))
                .Generate();

            dbContext.AddRange(testPerson, testProject, testContract);
            dbContext.SaveChanges();
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }
    }
}
