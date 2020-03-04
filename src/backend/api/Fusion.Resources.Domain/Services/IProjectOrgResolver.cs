using Fusion.ApiClients.Org;
using System;
using System.Threading.Tasks;


#nullable enable
namespace Fusion.Resources.Domain
{
    public interface IProjectOrgResolver
    {
        Task<ApiBasePositionV2?> ResolveBasePositionAsync(Guid basePositionId);
    }
}
