using FluentAssertions;

namespace Fusion.Infra.Cli.Tests
{
    public class ProvisionCommandTests
    {
        [Fact]
        public async Task ProvisionCommand_ShouldOverrideEnvironment()
        {

            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision -e {randomEnv} --url https://localhost -f required");

            var request = result.Operations.FirstOrDefault();
            request.Should().NotBeNull();

            request!.Request?.Environment.Should().Be(randomEnv);
        }

        [Fact]
        public async Task ProvisionCommand_ShouldWait_ByDefault()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision --url https://localhost -f required");

            var request = result.Operations.FirstOrDefault();
            request.Should().NotBeNull();
            
            request!.Checks.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ProvisionCommand_ShouldNotWait_WhenNoWait()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision --no-wait --url https://localhost -f required");

            var request = result.Operations.First();
            request!.Checks.Should().Be(0);
        }

        [Fact]
        public async Task ProvisionCommand_ShouldAddOwner_FromCommandLine()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision --sql-owner 2020 --sql-owner 2021 --url https://localhost -f required");

            var request = result.Operations.First();
            request!.Request?.SqlPermission?.Owners.Should().Contain("2020");
            request!.Request?.SqlPermission?.Owners.Should().Contain("2021");
        }

        [Fact]
        public async Task ProvisionCommand_ShouldAddContributor_FromCommandLine()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision --sql-contributor 3030 --sql-contributor 3031 --url https://localhost -f required");

            var request = result.Operations.First();
            request!.Request?.SqlPermission?.Contributors.Should().Contain("3030");
            request!.Request?.SqlPermission?.Contributors.Should().Contain("3031");
        }


        [Fact]
        public async Task ProvisionCommand_ShouldAddPullRequestVariables()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision -pr 123 --repo equinor/my-repo --copy-from ci --url https://localhost -f db-config.json");

            var request = result.Operations.First();
            request!.Request?.Environment.Should().Be("pr", "Should set pr as default env when not overridden");
            request!.Request?.PullRequest?.PrNumber.Should().Be("123");
            request!.Request?.PullRequest?.GithubRepo.Should().Be("equinor/my-repo");
            request!.Request?.PullRequest?.CopyFromEnvironment.Should().Be("ci");
        }

        [Fact]
        public async Task ProvisionCommand_ShouldTargetProduction()
        {
            var randomEnv = $"{Guid.NewGuid()}";

            var testCommand = new TestCommandBuilder();

            var result = await testCommand.ExecuteCommand($"database provision --production --url https://localhost -f required");

            var request = result.Operations.First();
            request!.ProductionEnvironment.Should().BeTrue();
        }

    }
}