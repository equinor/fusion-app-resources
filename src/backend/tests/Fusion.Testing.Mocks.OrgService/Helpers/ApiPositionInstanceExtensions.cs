using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fusion.Testing.Mocks.OrgService
{
    public static class ApiPositionInstanceExtensions
    {
        public static ApiPositionInstanceV2 SetAssignedPerson(this ApiPositionInstanceV2 instance, string mail)
        {
            instance.AssignedPerson = new ApiPersonV2 { Mail = mail };
            return instance;
        }

        public static ApiPositionInstanceV2 SetAssignedPerson(this ApiPositionInstanceV2 instance, ApiPersonProfileV3 person)
        {
            instance.AssignedPerson = new ApiPersonV2 { AzureUniqueId = person.AzureUniqueId, Mail = person.Mail };
            return instance;
        }

        public static ApiPositionInstanceV2 SetExternalId(this ApiPositionInstanceV2 instance, string externalId)
        {
            instance.ExternalId = externalId;
            return instance;
        }
        public static ApiPositionInstanceV2 SetParentPosition(this ApiPositionInstanceV2 instance, Guid? parentPositionId)
        {
            instance.ParentPositionId = parentPositionId;
            return instance;
        }
        public static ApiPositionInstanceV2 AddTaskOwner(this ApiPositionInstanceV2 instance, Guid positionId)
        {
            if (instance.TaskOwnerIds == null)
                instance.TaskOwnerIds = new List<Guid>();
            instance.TaskOwnerIds.Add(positionId);
            return instance;
        }

        public static ApiPositionInstanceV2 AddTaskOwner(this ApiPositionInstanceV2 instance, params Guid[] positionIds)
        {
            if (instance.TaskOwnerIds == null)
                instance.TaskOwnerIds = new List<Guid>();

            instance.TaskOwnerIds.AddRange(positionIds);
            return instance;
        }

        public static ApiPositionInstanceV2 Clone(this ApiPositionInstanceV2 instance)
        {
            return JsonConvert.DeserializeObject<ApiPositionInstanceV2>(JsonConvert.SerializeObject(instance));
        }

    }
}
