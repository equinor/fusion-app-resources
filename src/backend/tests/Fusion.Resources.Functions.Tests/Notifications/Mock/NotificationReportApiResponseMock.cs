using System;
using System.Collections.Generic;
using Fusion.ApiClients.Org;
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

    public static List<IResourcesApiClient.InternalPersonnelPerson> GetMockedInternalPersonnel(
        double personnelCount,
        double workload,
        double otherTasks,
        double vacationLeave,
        double absenceLeave)
    {
        var personnel = new List<IResourcesApiClient.InternalPersonnelPerson>();
        for (var i = 0; i < personnelCount; i++)
        {
            personnel.Add(new IResourcesApiClient.InternalPersonnelPerson()
            {
                EmploymentStatuses = new List<IResourcesApiClient.ApiPersonAbsence>
                    {
                        new()
                        {
                            Type = IResourcesApiClient.ApiAbsenceType.Vacation,
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AbsencePercentage = vacationLeave
                        },
                        new()
                        {
                            Type = IResourcesApiClient.ApiAbsenceType.OtherTasks,
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AbsencePercentage = otherTasks
                        },
                        new()
                        {
                            Type = IResourcesApiClient.ApiAbsenceType.Absence,
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AbsencePercentage = absenceLeave
                        }
                    },
                PositionInstances = new List<IResourcesApiClient.PersonnelPosition>
                    {
                        new()
                        {
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            Workload = workload,
                        }
                    }
            }
            );
        }

        return personnel;
    }
}