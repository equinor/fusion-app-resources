using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using Moq;
using Xunit;

#nullable enable

namespace Fusion.Resources.Domain.Tests;

public class OrgUnitResolverTests
{
    private readonly ResolveLineOrgUnit.Handler handler;

    public OrgUnitResolverTests()
    {
        var mockResolver = new Mock<ILineOrgResolver>();
        mockResolver.Setup(r => r
            .ResolveOrgUnitAsync(It.IsAny<DepartmentId>()))
            .Returns(ResolveOrgUnitAsync);
        var lineOrgResolver = mockResolver.Object;
        handler = new(lineOrgResolver);
    }

    private Task<ApiOrgUnit?> ResolveOrgUnitAsync(DepartmentId identifier)
    {
        var result = identifier.Type switch
        {
            DepartmentId.DepartmentIdType.SapId => new ApiOrgUnit { SapId = identifier.SapId! },
            DepartmentId.DepartmentIdType.FullPath => new ApiOrgUnit { Department = identifier.FullPath },
            DepartmentId.DepartmentIdType.LocalPath => new ApiOrgUnit { Department = identifier.LocalPath },
            _ => null,
        };
        return Task.FromResult(result);
    }

    [Fact]
    public async Task ShouldResolveSapIdAsDepartmentId()
    {
        var correctSapId = "12345678";
        var resolver = new ResolveLineOrgUnit(correctSapId);
        var result = await handler.Handle(resolver, CancellationToken.None);
        result.Should().NotBeNull();
        result?.SapId.Should().Be(correctSapId);
    }

    [Fact]
    public async Task ShouldResolveWorkdayIdAsDepartmentId()
    {
        var correctWorkdayId = "SO123456";
        var resolver = new ResolveLineOrgUnit(correctWorkdayId);
        var result = await handler.Handle(resolver, CancellationToken.None);
        result.Should().NotBeNull();
        result?.SapId.Should().Be(correctWorkdayId);
    }

    [Fact]
    public async Task ShouldResolveDepartmentNameAsFullPath()
    {
        var path = "ABC DEF";
        var resolver = new ResolveLineOrgUnit(path);
        var result = await handler.Handle(resolver, CancellationToken.None);
        result.Should().NotBeNull();
        result?.Department.Should().Be(path);
    }
}