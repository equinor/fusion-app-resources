using System;
using Fusion.ApiClients.Org;
using System.Collections.Generic;

#nullable enable

namespace Fusion.Testing.Mocks
{

    public class TestApiInternalRequestModel
    {
        public Guid Id { get; set; }
        public long Number { get; set; }
        public string? AssignedDepartment { get; set; }
        public string? Discipline { get; set; }
        public string? State { get; set; }

        /// <summary>Type of request
        /// <para>Check valid values used in request model <see cref="ApiAllocationRequestType"/> for information.</para>
        /// </summary>
        public string Type { get; set; } = null!;
        public TestApiProjectReference Project { get; set; } = null!;
        public ApiPositionV2? OrgPosition { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiPositionInstanceV2? OrgPositionInstance { get; set; }
        public Guid? OrgPositionInstanceId { get; set; }
        public string? AdditionalNote { get; set; }

        public Dictionary<string, object>? ProposedChanges { get; set; }
        public Guid? ProposedPersonAzureUniqueId { get; set; }
        public TestApiProposedPerson? ProposedPerson { get; set; }

        //public ApiTaskOwner? TaskOwner { get; set; }

        public DateTimeOffset Created { get; set; }
        public TestApiPerson CreatedBy { get; set; } = null!;

        public DateTimeOffset? Updated { get; set; }
        public TestApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }

        public List<TestApiComment>? Comments { get; set; }
        public TestApiTaskOwner? TaskOwner { get; set; }

        public TestApiWorkflow Workflow { get; set; }
    }
}
