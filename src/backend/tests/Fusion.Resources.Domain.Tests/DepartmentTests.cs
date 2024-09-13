using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands.Departments;
using Fusion.Resources.Test.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fusion.Resources.Domain.Tests;

public class DepartmentTests : DbTestFixture
{
    [Fact]
    public async Task ArchiveDelegatedResourceOwners_ShouldArchiveResourceOwners()
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var db = serviceProvider.GetRequiredService<ResourcesDbContext>();

        var departmentId = new LineOrgId()
        {
            FullDepartment = "PRD TDI",
            SapId = "123456"
        };
        var toBeArchivedResourceOwner = CreateTestPerson("toBeArchivedResourceOwner");
        var resourceOwner = CreateTestPerson("resourceOwner");

        var createCommand = new AddDelegatedResourceOwner(departmentId, toBeArchivedResourceOwner.AzureUniqueId)
        {
            Reason = "Test",
            DateFrom = DateTimeOffset.Now,
            DateTo = DateTimeOffset.Now.AddMonths(1)
        };


        var createCommand2 = new AddDelegatedResourceOwner(departmentId, resourceOwner.AzureUniqueId)
        {
            Reason = "Test2",
            DateFrom = DateTimeOffset.Now,
            DateTo = DateTimeOffset.Now.AddMonths(1)
        };

        await mediator.Send(createCommand);
        await mediator.Send(createCommand2);

        var command = new ArchiveDelegatedResourceOwners(departmentId).WhereResourceOwnersAzureId([toBeArchivedResourceOwner.AzureUniqueId]);

        await mediator.Send(command);

        var remainingResourceOwners = await db.DelegatedDepartmentResponsibles
            .Where(r => r.DepartmentId == departmentId.FullDepartment)
            .ToListAsync();

        remainingResourceOwners.Should().HaveCount(1);
        remainingResourceOwners.Should().ContainSingle(r => r.ResponsibleAzureObjectId == resourceOwner.AzureUniqueId);

        var archivedResourceOwners = await db.DelegatedDepartmentResponsiblesHistory.ToListAsync();

        archivedResourceOwners.Should().HaveCount(1);
        archivedResourceOwners.Should().ContainSingle(r => r.ResponsibleAzureObjectId == toBeArchivedResourceOwner.AzureUniqueId);
    }
}