using FluentAssertions;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Xunit;
#nullable enable
namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ModelTests
    {

        [Theory]
        [InlineData("L1", 1)]
        [InlineData("L1 L2", 2)]
        [InlineData("L1 L2 L3", 3)]
        [InlineData("L1 L2 L3 L4", 4)]
        [InlineData("L1 L2 L3 L4 L5", 5)]
        [InlineData("L1 L2 L3 L4 L5 ", 5)]
        public void DepartmentPath_ShouldIdentifiyCorrectLevel(string path, int expectedLevel)
        {
            var dep = new DepartmentPath(path);
            dep.Level.Should().Be(expectedLevel);
        }

        [Fact]
        public void DepartmentPath_ShouldReturnCorrectPath_WhenGoTo()
        {
            var dep = new DepartmentPath("L1 L2 L3 L4");
            dep.GoToLevel(2).Should().Be("L1 L2");
        }

        [Fact]
        public void DepartmentPath_ShouldReturnCorrectParent()
        {
            var dep = new DepartmentPath("L1 L2 L3 L4");
            dep.Parent().Should().Be("L1 L2 L3");
        }

        [Fact]
        public void DepartmentPath_ShouldReturnCorrectParent_WhenJumpingLevel()
        {
            var dep = new DepartmentPath("L1 L2 L3 L4");
            dep.Parent(2).Should().Be("L1 L2");
        }
    }
}
