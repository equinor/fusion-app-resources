using FluentAssertions;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;
using Fusion.Resources.Functions.Tests.Notifications.Mock;
using Xunit;

namespace Fusion.Resources.Functions.Tests.Notifications;

public class ScheduledReportNotificationBuilderTests
{
    [Fact]
    public void GetProjectChanges_ShouldReturnCountOfRelevantEventsForNextThreeMonths()
    {
        // Arrange
        var events = NotificationReportApiResponseMock.GetMockedChangeLogEvents();

        // Act
        var changes = ResourceOwnerReportDataCreator.GetProjectChangesAffectingNextThreeMonths(events);

        // Assert
        changes.Should().Be(4);
    }
}