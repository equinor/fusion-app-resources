﻿using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryProvisioningStatus
    {
        public QueryProvisioningStatus(DbContractorRequest.ProvisionStatus status)
        {
            State = status.State.ToString();
            PositionId = status.PositionId;
            Provisioned = status.Provisioned;
            ErrorMessage = status.ErrorMessage;
            ErrorPayload = status.ErrorPayload;
        }

        public QueryProvisioningStatus(DbResourceAllocationRequest.DbOpProvisionStatus status)
        {
            State = status.State.ToString();
            PositionId = status.OrgPositionId;
            Provisioned = status.Provisioned;
            ErrorMessage = status.ErrorMessage;
            ErrorPayload = status.ErrorPayload;
        }

        public string State { get; set; } = null!;
        public Guid? PositionId { get; set; }
        public DateTimeOffset? Provisioned { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorPayload { get; set; }
    }
}

