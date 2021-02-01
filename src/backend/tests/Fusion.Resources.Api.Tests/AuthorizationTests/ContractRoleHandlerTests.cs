using FluentAssertions;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Authorization.Handlers;
using Fusion.Resources.Test;
using Microsoft.AspNetCore.Authorization;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Fusion.Authorization;
using Xunit;

namespace Fusion.Resources.Api.Tests
{

    public class ContractRoleHandlerTests
    {
        private readonly Guid userAzureUniqueId;
        private readonly Guid projectId;
        private readonly Guid contractId;
        private readonly string projectName;
        private readonly Controllers.ProjectIdentifier project;

        public ContractRoleHandlerTests()
        {
            userAzureUniqueId = Guid.NewGuid();
            projectId = Guid.NewGuid();
            contractId = Guid.NewGuid();
            projectName = $"Test project {projectId}";
            project = new Controllers.ProjectIdentifier($"{projectId}", projectId, projectName);
        }



        [Fact]
        public async Task AnyInternalRole_Should_HaveAccess_When_CompanyRep()
        {
            var contract = ApiContractBuilder.NewContract(contractId)
                .WithCompanyRep(userAzureUniqueId);

            var orgResolver = new Mock<IProjectOrgResolver>();
            orgResolver.Setup(r => r.ResolveContractAsync(projectId, contractId)).ReturnsAsync(contract);

            var requirement = ContractRole.AnyInternalRole;
            var resource = new ContractResource(project.ProjectId, contractId);
            var context = GetContext(requirement, resource, GetClaimsUser(userAzureUniqueId));
            var handler = new ContractRoleAuthHandlerTester(orgResolver.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task AnyInternalRole_Should_NotHaveAccess_When_ExternalCompanyRep()
        {
            var contract = ApiContractBuilder.NewContract(contractId)
                .WithExternalCompanyRep(userAzureUniqueId);

            var orgResolver = new Mock<IProjectOrgResolver>();
            orgResolver.Setup(r => r.ResolveContractAsync(projectId, contractId)).ReturnsAsync(contract);

            var requirement = ContractRole.AnyInternalRole;
            var resource = new ContractResource(project.ProjectId, contractId);
            var context = GetContext(requirement, resource, GetClaimsUser(userAzureUniqueId));
            var handler = new ContractRoleAuthHandlerTester(orgResolver.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task AnyRole_Should_HaveAccess_When_ExternalCompanyRep()
        {
            var contract = ApiContractBuilder.NewContract(contractId)
                .WithExternalCompanyRep(userAzureUniqueId);

            var orgResolver = new Mock<IProjectOrgResolver>();
            orgResolver.Setup(r => r.ResolveContractAsync(projectId, contractId)).ReturnsAsync(contract);

            var requirement = ContractRole.Any;
            var resource = new ContractResource(project.ProjectId, contractId);
            var context = GetContext(requirement, resource, GetClaimsUser(userAzureUniqueId));
            var handler = new ContractRoleAuthHandlerTester(orgResolver.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task AnyRole_Should_HaveAccess_When_ExternalContractRep()
        {
            var contract = ApiContractBuilder.NewContract(contractId)
                .WithExternalContractRep(userAzureUniqueId);

            var orgResolver = new Mock<IProjectOrgResolver>();
            orgResolver.Setup(r => r.ResolveContractAsync(projectId, contractId)).ReturnsAsync(contract);

            var requirement = ContractRole.Any;
            var resource = new ContractResource(project.ProjectId, contractId);
            var context = GetContext(requirement, resource, GetClaimsUser(userAzureUniqueId));
            var handler = new ContractRoleAuthHandlerTester(orgResolver.Object);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        private AuthorizationHandlerContext GetContext(IAuthorizationRequirement requirement, object resource, ClaimsPrincipal user)
            => new AuthorizationHandlerContext(new[] { requirement }, user, resource);

        private ClaimsPrincipal GetClaimsUser(Guid azureId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(FusionClaimsTypes.AzureUniquePersonId, $"{azureId}")
                }));
        }

        private class ContractRoleAuthHandlerTester : ContractRoleAuthHandler
        {
            public ContractRoleAuthHandlerTester(IProjectOrgResolver orgResolver) : base(orgResolver)
            {
            }
        }

    }
}
