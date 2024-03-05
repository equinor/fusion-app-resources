using System;
using System.Collections.Generic;
using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Resources.Functions.ApiClients;
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

    [Fact]
    public void GetCapacityInUse_ShouldReturnCorrectCapacityInUse()
    {
        // Arrange
        const int personnelCount = 4;
        const int workload = 80;
        const int otherTasks = 4;
        const int vacationLeave = 2;
        const int absenceLeave = 3;
        var personnel = NotificationReportApiResponseMock.GetMockedInternalPersonnel(
            personnelCount,
            workload,
            otherTasks,
            vacationLeave,
            absenceLeave);

        // Act
        var capacityInUse = ResourceOwnerReportDataCreator.GetCapacityInUse(personnel);
        var capacityInUseCalculated = (int)Math.Round((double)(workload + otherTasks) /
            (100 - (vacationLeave + absenceLeave)) * 100);

        // Assert
        capacityInUse.Should().Be(capacityInUseCalculated);
    }

    [Fact]
    public void GetNumberOfRequestsLastWeek_ShouldReturnCorrectNumberOfRequests()
    {
        // Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            // Will pass
            new()
            {
                Type = RequestType.Allocation.ToString(),
                IsDraft = false,
                Created = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                IsDraft = false,
                Created = DateTimeOffset.UtcNow.AddDays(-8)
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                IsDraft = true,
                Created = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                IsDraft = false,
                Created = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        // Act
        var numberOfRequests = ResourceOwnerReportDataCreator.GetNumberOfRequestsLastWeek(requests);

        // Assert
        numberOfRequests.Should().Be(1);
    }

    [Fact]
    public void GetNumberOfOpenRequests_ShouldReturnCorrectNumberOfOpenRequests()
    {
        // Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            // Will pass
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Created.ToString(),
                ProposedPerson = new IResourcesApiClient.ProposedPerson()
                    { Person = new IResourcesApiClient.InternalPersonnelPerson() { AzureUniquePersonId = new Guid() } },
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Created.ToString(),
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Completed.ToString(),
            },
        };


        // Act
        var numberOfOpenRequests = ResourceOwnerReportDataCreator.GetNumberOfOpenRequests(requests);

        // Assert
        numberOfOpenRequests.Should().Be(1);
    }

    [Fact]
    public void GetNumberOfRequestsStartingInMoreThanThreeMonths_ShouldReturnCorrectNumberOfRequests()
    {
        // Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            // Will pass
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) },
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) },
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Completed.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) },
            },
        };


        // Act
        var numberOfRequests =
            ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInMoreThanThreeMonths(requests);

        // Assert
        numberOfRequests.Should().Be(1);
    }

    [Fact]
    public void GetTotalNumberOfPersonnel_ShouldReturnCorrectNumberOfPersonnel()
    {
        // Arrange
        var personnel = NotificationReportApiResponseMock.GetMockedInternalPersonnel(5, 100, 0, 0, 0);

        // Act
        var totalNumberOfPersonnel = ResourceOwnerReportDataCreator.GetTotalNumberOfPersonnel(personnel);

        // Assert
        totalNumberOfPersonnel.Should().Be(5);
    }

    [Fact]
    public void GetNumberOfRequestsStartingInLessThanThreeMonths_ShouldReturnCorrectNumberOfRequests()
    {
        // Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            // Will pass
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) },
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) },
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Completed.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) },
            },
        };

        // Act
        var numberOfRequests =
            ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInLessThanThreeMonths(requests);

        // Assert
        numberOfRequests.Should().Be(1);
    }

    [Fact]
    public void GetAllocationChangesAwaitingTaskOwnerAction_ShouldReturnCorrectNumberOfRequests()
    {
        // Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            // Will pass
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Created.ToString(),
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Completed.ToString(),
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
            },
        };

        // Act
        var numberOfRequests =
            ResourceOwnerReportDataCreator.GetAllocationChangesAwaitingTaskOwnerAction(requests);

        // Assert
        numberOfRequests.Should().Be(1);
    }
}