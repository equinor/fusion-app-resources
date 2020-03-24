using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


#nullable enable
namespace Fusion.Resources.Domain
{
    public interface IProjectOrgResolver
    {
        Task<ApiBasePositionV2?> ResolveBasePositionAsync(Guid basePositionId);
        Task<ApiProjectContractV2?> ResolveContractAsync(Guid projectId, Guid contractId);
        Task<ApiPositionV2?> ResolvePositionAsync(Guid positionId);

        /// <summary>
        /// Resolve all positions in the id list. 
        /// Those not located will not be in the returned list -> FirstOrDefault(p => p.Id == myPosId) == null
        /// </summary>
        /// <param name="positionIds"></param>
        /// <returns></returns>
        Task<IEnumerable<ApiPositionV2>> ResolvePositionsAsync(IEnumerable<Guid> positionIds);
    }
}
