﻿using Fusion.ApiClients.Org;
using Fusion.Resources.Api.Controllers.Departments;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResourceAllocationRequest
    {
        public ApiResourceAllocationRequest(QueryResourceAllocationRequest query)
        {
            Id = query.RequestId;
            Number = query.RequestNumber;

            AssignedDepartment = query.AssignedDepartment;
            if (query.AssignedDepartmentDetails is not null)
                AssignedDepartmentDetails = new ApiDepartment(query.AssignedDepartmentDetails);

            Discipline = query.Discipline;
            State = query.State;
            Type = $"{query.Type}";
            SubType = query.SubType;

            if (query.ProposedPerson != null)
            {
                ProposedPerson = new ApiProposedPerson(query.ProposedPerson);
                ProposedPersonAzureUniqueId = query.ProposedPerson.AzureUniqueId;
            }

            Project = new ApiProjectReference(query.Project);

            OrgPositionId = query.OrgPositionId;
            if (query.OrgPosition != null)
                OrgPosition = query.OrgPosition;

            OrgPositionInstanceId = query.OrgPositionInstanceId;
            if (query.OrgPositionInstance != null)
                OrgPositionInstance = query.OrgPositionInstance;

            AdditionalNote = query.AdditionalNote;

            if (query.ProposedChanges is not null)
                ProposedChanges = new ApiPropertiesCollection(query.ProposedChanges);

            ProposalParameters = new ApiProposalParameters(query.ProposalParameters);

            if (query.TaskOwner != null)
                TaskOwner = new ApiTaskOwner(query.TaskOwner);

            Created = query.Created;
            Updated = query.Updated;
            CreatedBy = new ApiPerson(query.CreatedBy);

            UpdatedBy = ApiPerson.FromEntityOrDefault(query.UpdatedBy);

            LastActivity = query.LastActivity;
            IsDraft = query.IsDraft;

            if (query.Workflow != null) Workflow = new ApiWorkflow(query.Workflow);
            ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);
        }

        public Guid Id { get; set; }
        public long Number { get; set; }

        public string? AssignedDepartment { get; set; }
        public ApiDepartment? AssignedDepartmentDetails { get; }
        public string? Discipline { get; set; }
        public string? State { get; set; }
        /// <summary>Type of request
        /// <para>Check valid values used in request model <see cref="ApiAllocationRequestType"/> for information.</para>
        /// </summary>
        public string Type { get; set; }
        public string? SubType { get; set; }
        public ApiWorkflow? Workflow { get; set; }
        public ApiProjectReference Project { get; set; }
        public ApiPositionV2? OrgPosition { get; set; }
        public Guid? OrgPositionId { get; set; }
        public ApiPositionInstanceV2? OrgPositionInstance { get; set; }
        public Guid? OrgPositionInstanceId { get; set; }
        public string? AdditionalNote { get; set; }

        public ApiPropertiesCollection? ProposedChanges { get; set; }
        public Guid? ProposedPersonAzureUniqueId { get; set; }
        public ApiProposedPerson? ProposedPerson { get; set; }
        public ApiProposalParameters? ProposalParameters { get; set; }

        public ApiTaskOwner? TaskOwner { get; set; }

        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }

        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }
        public ApiProvisioningStatus ProvisioningStatus { get; set; }

        internal bool ShouldHideProposalsForProject
        {
            get
            {
                var isTypeAllocation = string.Equals(Type, "allocation", StringComparison.OrdinalIgnoreCase);
                var isNormalRequest = string.Equals(SubType, "normal", StringComparison.OrdinalIgnoreCase);
                var inCreatedState = string.Equals(State, "created", StringComparison.OrdinalIgnoreCase);

                return isTypeAllocation && isNormalRequest && inCreatedState;
            }
        }
        public ApiResourceAllocationRequest HideProposals()
        {
            ProposalParameters = null;
            ProposedChanges = null;
            ProposedPerson = null;
            ProposedPersonAzureUniqueId = null;
            return this;
        }
    }
}
