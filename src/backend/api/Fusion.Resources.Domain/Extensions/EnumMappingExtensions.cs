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
    }
}
