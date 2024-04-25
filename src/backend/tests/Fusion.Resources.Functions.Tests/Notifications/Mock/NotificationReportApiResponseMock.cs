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
                            Workload = workload,
                            AllocationState = null,
                            AllocationUpdated = null,
                        }
                    }
            }
            );
            ;
        }

        return personnel;
    }

    public static List<IResourcesApiClient.InternalPersonnelPerson> GetMockedInternalPersonnelWithAllocationUpdated(
      double personnelCount
      )
    {
        var personnel = new List<IResourcesApiClient.InternalPersonnelPerson>();
        for (var i = 0; i < personnelCount; i++)
        {
            personnel.Add(new IResourcesApiClient.InternalPersonnelPerson()
            {
                PositionInstances = new List<IResourcesApiClient.PersonnelPosition>
                    {
                        new()
                        {
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = null,
                            AllocationUpdated = null,
                        },
                        new()
                        {
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = "ChangeByTaskOwner",
                            AllocationUpdated = DateTime.UtcNow,
                        },
                        new()                           
                        {
                            AppliesFrom = DateTime.UtcNow.AddDays(-1 - i),
                            AppliesTo = DateTime.UtcNow.AddDays(1 + i * 10),
                            AllocationState = "ChangeByTaskOwner",
                            AllocationUpdated = DateTime.UtcNow.AddDays(-10),
                        },
                        new()                           
                        {
                            AppliesFrom = DateTime.UtcNow.AddDays(91),
                            AppliesTo = DateTime.UtcNow.AddDays(60 + i * 10),
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