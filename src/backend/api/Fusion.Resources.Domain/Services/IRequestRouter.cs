using Fusion.Resources.Database.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public interface IRequestRouter
    {
        Task<string?> RouteAsync(DbResourceAllocationRequest request, CancellationToken cancellationToken);
    }
}