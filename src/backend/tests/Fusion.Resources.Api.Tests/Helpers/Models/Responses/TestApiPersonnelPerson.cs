﻿using Fusion.Testing.Mocks;
using System;

namespace Fusion.Resources.Api.Tests
{
    public class TestApiPersonnelPerson
    {
        public Guid? azureUniquePersonId { get; set; }
        public string mail { get; set; }
        public string name { get; set; }
        public string phoneNumber { get; set; }
        public string jobTitle { get; set; }
        public object officeLocation { get; set; }
        public string department { get; set; }
        public object fullDepartment { get; set; }
        public bool isResourceOwner { get; set; }
        public string accountType { get; set; }
        public Employmentstatus[] employmentStatuses { get; set; }
        public TestTimeline[] timeline { get; set; }
        public TestPersonPosition[] positionInstances { get; set; }
        public TestApiInternalRequestModel[] pendingRequests { get; set; }
    }

    public class TestPersonPosition
    {
        public Guid PositionId { get; set; }

        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public bool HasChangeRequest { get; set; }
        public TestRequestStatus ChangeRequestStatus { get; set; }
    }

    public class TestRequestStatus
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public bool IsDraft { get; set; }
    }

    public class Employmentstatus
    {
        public Guid id { get; set; }
        public DateTime appliesFrom { get; set; }
        public DateTime? appliesTo { get; set; }
        public int absencePercentage { get; set; }
        public string type { get; set; }
        public TestTaskdetails taskDetails { get; set; }
    }

    public class TestTaskdetails
    {
        public bool isHidden { get; set; }
        public object basePositionId { get; set; }
        public string taskName { get; set; }
        public string roleName { get; set; }
        public object location { get; set; }
    }

    public class TestTimeline
    {
        public DateTime appliesFrom { get; set; }
        public DateTime appliesTo { get; set; }
        public TestTimeLineItem[] items { get; set; }
        public double? workload { get; set; }
    }

    public class TestTimeLineItem
    {
        public string id { get; set; }
        public string type { get; set; }
        public double? workload { get; set; }
        public string description { get; set; }
        public string roleName { get; set; }
        public string taskName { get; set; }
    }
}
