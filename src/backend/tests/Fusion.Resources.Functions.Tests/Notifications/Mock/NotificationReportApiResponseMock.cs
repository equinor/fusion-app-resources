using System;
using System.Collections.Generic;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

namespace Fusion.Resources.Functions.Tests.Notifications.Mock;

public abstract class NotificationReportApiResponseMock
{
    public static List<ApiChangeLogEvent> GetMockedChangeLogEvents()
    {
        // Loop through the ChangeType enum and create a list of ApiChangeLogEvent objects
        var events = new List<ApiChangeLogEvent>();
        foreach (var changeType in Enum.GetValues<ChangeType>())
        {
            events.Add(new ApiChangeLogEvent
            {
                ChangeType = changeType.ToString(),
                TimeStamp = DateTime.UtcNow
            });
        }

        return events;
    }
}