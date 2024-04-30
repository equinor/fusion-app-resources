using System;
using System.Collections.Generic;
using Fusion.ApiClients.Org;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

namespace Fusion.Resources.Functions.Tests.Notifications.Mock;

public abstract class NotificationReportApiResponseMock
{

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
                            Workload = workload
                        }
                    }
            }
            );
            ;
        }

        return personnel;
    }

    public static List<IResourcesApiClient.InternalPersonnelPerson> GetMockedInternalPersonnelWithInstancesWithAndWithoutChanges(double personnelCount)
    {
        var personnel = new List<IResourcesApiClient.InternalPersonnelPerson>();
        for (var i = 0; i < personnelCount; i++)
        {
            personnel.Add(new IResourcesApiClient.InternalPersonnelPerson()
            {
                // Should return 4 instances for each person
                PositionInstances = new List<IResourcesApiClient.PersonnelPosition>
                    {
                        new()
                        {
                            // One active instance without any changes
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = null,
                            AllocationUpdated = null,
                        },
                        new()
                        {
                            // One active instance that contains changes done within the last week
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = "ChangeByTaskOwner",
                            AllocationUpdated = DateTime.UtcNow,
                        },
                        new()
                        {
                            // One active instance that contains changes done more than a week ago
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = "ChangeByTaskOwner",
                            AllocationUpdated = DateTime.UtcNow.AddDays(-8),
                        },
                        new()
                        {
                            // One instance that will become active in more than 3 months that contains changes
                            AppliesFrom = DateTime.UtcNow.AddMonths(4),
                            AppliesTo = DateTime.UtcNow.AddMonths(4 + i),
                            AllocationState = "ChangeByTaskOwner",
                            AllocationUpdated = DateTime.UtcNow,
                        }
                    }
            }
            );
            ;
        }

        return personnel;
    }
}