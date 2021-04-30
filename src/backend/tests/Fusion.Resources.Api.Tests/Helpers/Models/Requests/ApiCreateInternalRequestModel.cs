using Bogus;
using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Fusion.Testing.Mocks
{
    /// <summary>
    /// Test model for creating a new request
    /// </summary>
    public class ApiCreateInternalRequestModel
    {
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }
        public bool? IsDraft { get; set; }

        // Not required unless created from the resource owner side. Change requests.
        internal Guid? OrgProjectId { get; set; }
        public Guid OrgPositionId { get; set; }
        public Guid OrgPositionInstanceId { get; set; }

        public string? AdditionalNote { get; set; }
        public Dictionary<string, object>? ProposedChanges { get; set; }


        public Guid? ProposedPersonAzureUniqueId { get; set; }

        public ApiCreateInternalRequestModel AsTypeResourceOwner(string? subType = null)
        {
            Type = "resourceOwnerChange";
            SubType = subType;
            return this;
        }

        public ApiCreateInternalRequestModel AsTypeNormal()
        {
            Type = "allocation";
            SubType = "normal";
            return this;
        }
        public ApiCreateInternalRequestModel AsTypeJointVenture()
        {
            Type = "allocation";
            SubType = "jointVenture";
            return this;
        }
        public ApiCreateInternalRequestModel AsTypeEnterprise()
        {
            Type = "allocation";
            SubType = "enterprise";
            return this;
        }
        public ApiCreateInternalRequestModel AsTypeDirect()
        {
            Type = "allocation";
            SubType = "direct";
            return this;
        }

        public ApiCreateInternalRequestModel WithPosition(ApiPositionV2 position, Guid? instanceId = null)
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
    
        public ApiCreateInternalRequestModel WithAdditionalNote(string note)
        {
            AdditionalNote = note;
            return this;
        }

        //public ApiCreateInternalRequestModel WithIsDraft(bool isDraft)
        //{
        //    IsDraft = IsDraft;
        //    return this;
        //}

    }


}
