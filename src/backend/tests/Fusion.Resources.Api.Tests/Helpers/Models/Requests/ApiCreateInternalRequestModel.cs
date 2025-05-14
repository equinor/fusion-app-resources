using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Services.Org.ApiModels;

#nullable enable

namespace Fusion.Testing.Mocks
{
    public abstract class ApiCreateInternalRequestModelBase
    {
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }
        public bool? IsDraft { get; set; }

        // Not required unless created from the resource owner side. Change requests.
        internal Guid? OrgProjectId { get; set; }
        public Guid OrgPositionId { get; set; }

        public string? AdditionalNote { get; set; }
        public Dictionary<string, object>? ProposedChanges { get; set; }

        public string? AssignedDepartment { get; set; }

        public Guid? ProposedPersonAzureUniqueId { get; set; }

        public ApiCreateInternalRequestModelBase AsTypeResourceOwner(string? subType = null)
        {
            Type = "resourceOwnerChange";
            SubType = subType;
            return this;
        }

        public ApiCreateInternalRequestModelBase AsTypeNormal()
        {
            Type = "allocation";
            SubType = "normal";
            return this;
        }
        public ApiCreateInternalRequestModelBase AsTypeJointVenture()
        {
            Type = "allocation";
            SubType = "jointVenture";
            return this;
        }
        public ApiCreateInternalRequestModelBase AsTypeEnterprise()
        {
            Type = "allocation";
            SubType = "enterprise";
            return this;
        }
        public ApiCreateInternalRequestModelBase AsTypeDirect()
        {
            Type = "allocation";
            SubType = "direct";
            return this;
        }

        public ApiCreateInternalRequestModelBase WithProposedPerson(ApiPersonProfileV3 person)
        {
            ProposedPersonAzureUniqueId = person.AzureUniqueId;
            return this;
        }
        public ApiCreateInternalRequestModelBase WithAssignedDepartment(string? department)
        {
            AssignedDepartment = department;
            return this;
        }

        public abstract ApiCreateInternalRequestModelBase WithPosition(ApiPositionV2 position, Guid? instanceId = null);

        public ApiCreateInternalRequestModelBase WithAdditionalNote(string note)
        {
            AdditionalNote = note;
            return this;
        }
    }

    /// <summary>
    /// Test model for creating a new request
    /// </summary>
    public class ApiCreateInternalRequestModel : ApiCreateInternalRequestModelBase
    {
        public Guid OrgPositionInstanceId { get; set; }

        public override ApiCreateInternalRequestModelBase WithPosition(ApiPositionV2 position, Guid? instanceId = null)
        {
            OrgPositionId = position.Id;

            if (instanceId != null)
            {
                OrgPositionInstanceId = position.Instances.First(i => i.Id == instanceId).Id;
            }
            else
            {
                var currentInstance = position.Instances
                    .Where(i => i.AppliesFrom <= DateTime.UtcNow.Date && i.AppliesTo >= DateTime.UtcNow.Date)
                    .FirstOrDefault();

                if (currentInstance is null)
                    currentInstance = position.Instances.First();

                OrgPositionInstanceId = currentInstance.Id;
            }

            return this;
        }

    }

    public class ApiTestBatchRequestModel : ApiCreateInternalRequestModel
    {
        public Guid[]? OrgPositionInstanceIds { get; set; }

        public override ApiCreateInternalRequestModel WithPosition(ApiPositionV2 position, Guid? instanceId = null)
        {
            OrgPositionId = position.Id;
            OrgPositionInstanceIds = position.Instances
                .Where(i => i.AppliesTo >= DateTime.UtcNow.Date)
                .Select(i => i.Id)
                .ToArray();

            return this;
        }
    }
}
