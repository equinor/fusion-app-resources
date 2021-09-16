using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public interface IRequestRouter
    {
        Task<string?> RouteAsync(DbResourceAllocationRequest request, CancellationToken cancellationToken);
        Task<string?> RouteAsync(ApiPositionV2 position, Guid? instanceId, CancellationToken cancellationToken);
    }
}