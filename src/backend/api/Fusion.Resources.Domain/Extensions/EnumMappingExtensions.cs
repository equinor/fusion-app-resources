using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public static class EnumMappingExtensions
    {
        public static DbInternalRequestType MapToDatabase(this InternalRequestType originalEnum)
        {
            return originalEnum switch
            {
                InternalRequestType.Allocation => DbInternalRequestType.Allocation,
                InternalRequestType.ResourceOwnerChange => DbInternalRequestType.ResourceOwnerChange,
                _ => throw new NotSupportedException($"Cannot map '{originalEnum}' to {nameof(DbInternalRequestType)}")
            };
        }

        public static InternalRequestType MapToDomain(this DbInternalRequestType originalEnum)
        {
            return originalEnum switch
            {
                DbInternalRequestType.Allocation => InternalRequestType.Allocation,
                DbInternalRequestType.ResourceOwnerChange => InternalRequestType.ResourceOwnerChange,
                _ => throw new NotSupportedException($"Cannot map '{originalEnum}' to {nameof(InternalRequestType)}")
            };
        }

        public static DbResourceAllocationRequest.DbChangeScope MapToDatabase(this ProposalChangeScope value)
        {
            return value switch
            {
                ProposalChangeScope.Default => DbResourceAllocationRequest.DbChangeScope.Default,
                ProposalChangeScope.InstanceOnly => DbResourceAllocationRequest.DbChangeScope.InstanceOnly,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(DbResourceAllocationRequest.DbChangeScope)}")
            };
        }
        public static ProposalChangeScope MapToDomain(this DbResourceAllocationRequest.DbChangeScope value)
        {
            return value switch
            {
                DbResourceAllocationRequest.DbChangeScope.Default => ProposalChangeScope.Default,
                DbResourceAllocationRequest.DbChangeScope.InstanceOnly => ProposalChangeScope.InstanceOnly,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(ProposalChangeScope)}")
            };
        }

        public static QueryTaskSource MapToDomain(this DbTaskSource value)
        {
            return value switch
            {
                DbTaskSource.ResourceOwner => QueryTaskSource.ResourceOwner,
                DbTaskSource.TaskOwner => QueryTaskSource.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(QueryTaskSource)}.")
            };
        }

        public static DbTaskSource MapToDatabase(this QueryTaskSource value)
        {
            return value switch
            {
                QueryTaskSource.ResourceOwner => DbTaskSource.ResourceOwner,
                QueryTaskSource.TaskOwner => DbTaskSource.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(DbTaskSource)}.")
            };
        }

        public static QueryTaskResponsible MapToDomain(this DbTaskResponsible value)
        {
            return value switch
            {
                DbTaskResponsible.ResourceOwner => QueryTaskResponsible.ResourceOwner,
                DbTaskResponsible.TaskOwner => QueryTaskResponsible.TaskOwner,
                DbTaskResponsible.Both => QueryTaskResponsible.Both,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(QueryTaskSource)}.")
            };
        }

        public static DbTaskResponsible MapToDatabase(this QueryTaskResponsible value)
        {
            return value switch
            {
                QueryTaskResponsible.ResourceOwner => DbTaskResponsible.ResourceOwner,
                QueryTaskResponsible.TaskOwner => DbTaskResponsible.TaskOwner,
                QueryTaskResponsible.Both => DbTaskResponsible.Both,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(DbTaskSource)}.")
            };
        }

        public static QueryMessageRecipient MapToDomain(this DbMessageRecipient value)
        {
            return value switch
            {
                DbMessageRecipient.ResourceOwner => QueryMessageRecipient.ResourceOwner,
                DbMessageRecipient.TaskOwner => QueryMessageRecipient.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(QueryTaskSource)}.")
            };
        }

        public static DbMessageRecipient MapToDatabase(this QueryMessageRecipient value)
        {
            return value switch
            {
                QueryMessageRecipient.ResourceOwner => DbMessageRecipient.ResourceOwner,
                QueryMessageRecipient.TaskOwner => DbMessageRecipient.TaskOwner,
                _ => throw new NotSupportedException($"Cannot map '{value}' to {nameof(DbTaskSource)}.")
            };
        }
    }
}
