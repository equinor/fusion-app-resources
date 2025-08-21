using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Summary.Functions.ReportCreator;
using Fusion.Summary.Functions.Tests.Notifications.Mock;

namespace Fusion.Summary.Functions.Tests.Notifications;

public class ScheduledReportNotificationBuilderTests
{
    [Fact]
    public void GetChangesForDepartment_Should_ResultInNumberOfChanges()
    {
        // Arrange
        const int personnelCount = 4;
        // Returns 4 different instances pr personnel
        var personnel = NotificationReportApiResponseMock.GetMockedInternalPersonnelWithInstancesWithAndWithoutChanges(
            personnelCount);

        // Act
        var changes = ResourceOwnerReportDataCreator.CalculateDepartmentChangesLastWeek(personnel);

        // Assert
        changes.Should().Be(8);
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
                State = RequestState.Created.ToString()
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Created.ToString(),
                ProposedPerson = new IResourcesApiClient.ProposedPerson()
                    { Person = new IResourcesApiClient.InternalPersonnelPerson() { AzureUniquePersonId = new Guid() } }
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Created.ToString()
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Completed.ToString()
            }
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
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) }
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) }
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Completed.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) }
            }
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
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) }
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(4) }
            },
            new()
            {
                Type = RequestType.ResourceOwnerChange.ToString(),
                State = RequestState.Completed.ToString(),
                OrgPositionInstance = new ApiPositionInstanceV2 { AppliesFrom = DateTime.UtcNow.AddMonths(2) }
            }
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
                State = RequestState.Created.ToString()
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Completed.ToString()
            },
            new()
            {
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString()
            }
        };

        // Act
        var numberOfRequests =
            ResourceOwnerReportDataCreator.GetAllocationChangesAwaitingTaskOwnerAction(requests);

        // Assert
        numberOfRequests.Should().Be(1);
    }

    [Fact]
    public void GetOpenRequestsWorkload_ShouldReturnCorrectCapacityRequested()
    {
        //Arrange
        var requests = new List<IResourcesApiClient.ResourceAllocationRequest>
        {
            new()
            {
                OrgPositionInstance = new ApiPositionInstanceV2 {Workload =  20},
                Type = RequestType.Allocation.ToString(),
                State = RequestState.Created.ToString()
            }
        };


        // Act
        var totalRequestedWorkload = ResourceOwnerReportDataCreator.GetCombinedOpenRequestsWorkload(requests);


        // Assert
        totalRequestedWorkload.Should().Be(20);
    }
}
