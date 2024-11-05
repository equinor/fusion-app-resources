using FluentAssertions;
using Fusion.Summary.Functions.ReportCreator;

namespace Fusion.Summary.Functions.Tests.Notifications;

public class WeeklyTaskOwnerReportDataCreatorTests
{
    [Fact]
    public void ActiveAdmins_AreConsideredExpiring_IfValidToIsLessThanThreeMonths()
    {
        var now = DateTime.UtcNow;
        WeeklyTaskOwnerReportDataCreator.NowDate = now;

        var admins = new List<PersonAdmin>()
        {
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(1))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(90))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(120))), // Is not expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(365))) // Is not expiring
        };

        var data = WeeklyTaskOwnerReportDataCreator.GetExpiringAdmins(admins);

        data.Should().HaveCount(2);
    }


    [Fact]
    public void GetPositionsEndingNextThreeMonthsTest()
    {
        // throw new NotImplementedException();
    }

    [Fact]
    public void GetTBNPositionsStartingWithinThreeMontsTests()
    {
        // throw new NotImplementedException();
    }
}